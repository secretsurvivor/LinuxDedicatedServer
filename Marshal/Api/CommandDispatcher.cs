using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;

namespace LinuxDedicatedServer.Api;



public interface ICommandDispatcher
{
    public Task<ICommandResult> ExecuteCommand(Command command);
}

public interface IDispatcherConfig
{
    public IDispatcherConfig RegisterController<TController>() where TController : CommandController;

    public IDispatcherConfig RegisterControllerFromAssembly<TAssembly>() where TAssembly : class;
}

public interface ICommandAdaptor
{
    public Task<ICommandResult> Execute(CommandController instance, IEnumerable<CommandArgument> args);
}

public interface ICommandResult;

public abstract class CommandController
{
    public abstract Task<ICommandResult> Default(IEnumerable<CommandArgument> args);
}

public class CommandDispatcher(IServiceProvider provider, CommandDispatcherConfig config) : ICommandDispatcher
{
    private readonly ConcurrentDictionary<string, ControllerModel> cache = BuildCache(config);

    public async Task<ICommandResult> ExecuteCommand(Command command)
    {
        if (!cache.TryGetValue(command.GroupName, out var controller))
        {
            throw new ArgumentException($"Invalid Group identifier '{command.GroupName}'");
        }

        return await controller.Execute(provider, command);
    }

    private static ConcurrentDictionary<string, ControllerModel> BuildCache(CommandDispatcherConfig config)
    {
        var cache = new ConcurrentDictionary<string, ControllerModel>();

        foreach (var contr in config.ControllerFactories)
        {
            cache.TryAdd(contr.Attribute.GroupName, new ControllerModel(contr));
        }

        return cache;
    }
}

public static class CommandDispatcherServiceCollection
{
    public static IServiceCollection AddCommandDispatcher(this IServiceCollection services, Action<IDispatcherConfig> cfg)
    {
        var config = new CommandDispatcherConfig();
        cfg(config);

        foreach (var contr in config.ControllerFactories)
        {
            services.AddTransient(contr.Type);
        }

        services.AddSingleton(config);
        services.AddSingleton<ICommandDispatcher, CommandDispatcher>();

        return services;
    }

    public static IServiceCollection AddCommandDispatcher<TAssembly>(this IServiceCollection services) where TAssembly : class
    {
        return AddCommandDispatcher(services, cfg => cfg.RegisterControllerFromAssembly<TAssembly>());
    }
}

public class CommandDispatcherConfig : IDispatcherConfig
{
    public List<ControllerFactory> ControllerFactories { get; } = [];

    public IDispatcherConfig RegisterController<TController>() where TController : CommandController
    {
        var type = typeof(TController);
        var attr = type.GetCustomAttribute<CommandControllerAttribute>();

        if (attr is null)
        {
            throw new InvalidOperationException($"Controller '{type.Name}' is missing attribute 'CommandControllerAttribute'");
        }

        ControllerFactories.Add(new ControllerFactory(attr, type));

        return this;
    }

    public IDispatcherConfig RegisterControllerFromAssembly<TAssembly>() where TAssembly : class
    {
        var assembly = typeof(TAssembly).Assembly;
        var derivedTypes = assembly.GetTypes().Where(x => !x.IsAbstract && typeof(CommandController).IsAssignableFrom(x));

        foreach (var derivedType in derivedTypes)
        {
            var attr = derivedType.GetCustomAttribute<CommandControllerAttribute>();

            if (attr is null)
            {
                throw new InvalidOperationException($"Controller '{derivedType.Name}' is missing attribute 'CommandControllerAttribute'");
            }

            ControllerFactories.Add(new ControllerFactory(attr, derivedType));
        }

        return this;
    }
}

public class ControllerFactory(CommandControllerAttribute attr, Type type)
{
    public CommandControllerAttribute Attribute { get; } = attr;
    public Type Type { get; } = type;

    public bool TryCreateInstance(IServiceProvider provider, out CommandController instance)
    {
        if (ActivatorUtilities.CreateInstance(provider, Type) is CommandController controller)
        {
            instance = controller;
            return true;
        }

        instance = default!;
        return false;
    }

    public IEnumerable<CommandModel> GetCommands()
    {
        return Type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => x.GetCustomAttribute<CommandAttribute>() is not null)
            .Select(x => new CommandModel(x.GetCustomAttribute<CommandAttribute>()!, x));
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class CommandControllerAttribute(string groupName) : Attribute
{
    public string GroupName { get; init; } = groupName;
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class CommandAttribute(string actionName) : Attribute
{
    public string ActionName { get; init; } = actionName;
}

public class CommandArgumentAttribute<T>(string key) : Attribute
{

}

public record CommandModel(CommandAttribute Attribute, MethodInfo Method)
{
    public bool Valid()
    {
        var parameters = Method.GetParameters();

        return parameters.Length == 1
            && parameters.First().ParameterType == typeof(IEnumerable<CommandArgument>)
            && (Method.ReturnType == typeof(IEnumerable<CommandArgument>) 
            || (Method.ReturnType.IsGenericType
            && Method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
            && typeof(ICommandResult).IsAssignableFrom(Method.ReturnType.GetGenericArguments().First())));
    }
}

public class ControllerModel(ControllerFactory factory)
{
    private readonly ConcurrentDictionary<string, ICommandAdaptor> methodCache = BuildCache(factory);

    public string GroupName { get => factory.Attribute.GroupName; }

    public Task<ICommandResult> Execute(IServiceProvider provider, Command command)
    {
        if (!factory.TryCreateInstance(provider, out var instance))
        {
            throw new InvalidOperationException($"Failed to initiate controller '{factory.Type.Name}'");
        }

        if (command.ActionName is null)
        {
            return instance.Default(command.Arguments);
        }

        if (!methodCache.TryGetValue(command.ActionName, out var result))
        {
            throw new ArgumentException($"Action '{command.ActionName}' doesn't exist in {GroupName}");
        }

        return result.Execute(instance, command.Arguments);
    }

    private static ConcurrentDictionary<string, ICommandAdaptor> BuildCache(ControllerFactory factory)
    {
        var cache = new ConcurrentDictionary<string, ICommandAdaptor>();

        foreach (var command in factory.GetCommands())
        {
            if (!command.Valid())
            {
                throw new InvalidOperationException($"Invalid command: {command.Method.DeclaringType?.FullName}.{command.Method.Name} must have signature Task<ICommandResult>(IEnumerable<CommandArgument>)");
            }

            cache.TryAdd(command.Attribute.ActionName, new CommandAdaptor(command));
        }

        return cache;
    }
}

public class CommandAdaptor(CommandModel model) : ICommandAdaptor
{
    public Task<ICommandResult> Execute(CommandController instance, IEnumerable<CommandArgument> args)
    {
        return (model.Method.Invoke(instance, [args]) as Task<ICommandResult>)!;
    }
}