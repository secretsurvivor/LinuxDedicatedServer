using LinuxDedicatedServer.Api.Buffer.v1.Resolvers;
using System.Collections.Concurrent;

namespace LinuxDedicatedServer.Api.Buffer.v1;

public static class ManagedTypeResolver
{
    private readonly static ConcurrentDictionary<Type, IManagedTypeResolver> _managedResolvers = [];

    static ManagedTypeResolver()
    {
        RegisterResolver<string>(new StringResolver());
    }

    public static void RegisterResolver<T>(IManagedTypeResolver resolver)
    {
        if (_managedResolvers.ContainsKey(typeof(T)))
        {
            throw new InvalidOperationException("Resolver of this type already exists");
        }

        _managedResolvers.AddOrUpdate(typeof(T), resolver, (t, r) => resolver);
    }

    public static bool Resolve(Type type, out IManagedTypeResolver resolver)
    {
        return _managedResolvers.TryGetValue(type, out resolver!);
    }

    public static bool Resolve<T>(out IManagedTypeResolver resolver)
    {
        return _managedResolvers.TryGetValue(typeof(T), out resolver!);
    }

    public static bool ResolverExists(Type type)
    {
        return _managedResolvers.ContainsKey(type);
    }
}

public interface IManagedTypeResolver
{
    public (object value, int length) ConvertValue(object value);
    public void WriteAddValue(BufferFactory factory, Type type, object value);
    public void WriteValue(BufferWriter writer, Type type, object value);
    public void ReadAddValue(BufferFactory factory, Type type, int length);
    public object ReadValue(BufferReader reader, Type type, int length);
}
