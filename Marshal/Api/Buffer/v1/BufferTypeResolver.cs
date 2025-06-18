using LinuxDedicatedServer.Api.Buffer.v1.Resolvers;
using System.Collections.Concurrent;

namespace LinuxDedicatedServer.Api.Buffer.v1;

public static class BufferTypeResolver
{
    private static readonly ConcurrentDictionary<Type, IBufferResolver> _resolvers = [];

    static BufferTypeResolver()
    {
        RegisterResolver(new DateTimeResolver());
        RegisterResolver(new GuidResolver());
    }

    public static void RegisterResolver<T>(IBufferTypeResolver<T> resolver)
    {
        var type = typeof(T);

        if (_resolvers.ContainsKey(type))
        {
            throw new ArgumentException($"Resolver of this type already exists: {type.FullName}");
        }

        _resolvers.AddOrUpdate(type, resolver, (t, b) => resolver);
    }

    public static void Validate(Type type)
    {
        if (type.Equals(typeof(nint)) || type.Equals(typeof(nuint)))
        {
            throw new NotSupportedException($"Operation does not support {type.Name} primitive type");
        }
    }

    public static bool ValidateAndResolve(Type type, out IBufferResolver resolver)
    {
        if (!type.IsPrimitive)
        {
            if (_resolvers.TryGetValue(type, out resolver!))
            {
                return true;
            }

            throw new NotSupportedException("Primitive types are only directly supported and a resolver hasn't been registered");
        }

        Validate(type);
        resolver = default!;

        return false;
    }

    public static bool ValidateAndResolve<T>(out IBufferTypeResolver<T> resolver)
    {
        var type = typeof(T);

        if (!type.IsPrimitive)
        {
            if (_resolvers.TryGetValue(type, out var genericResolver))
            {
                resolver = (IBufferTypeResolver<T>)genericResolver;
                return true;
            }

            throw new NotSupportedException("Primitive types are only directly supported and a resolver hasn't been registered");
        }

        Validate(type);
        resolver = default!;

        return false;
    }

    public static bool IsSupportedType(Type type)
    {
        if (!type.IsPrimitive)
        {
            return _resolvers.TryGetValue(type, out _);
        }

        if (type.Equals(typeof(nint)) || type.Equals(typeof(nuint)))
        {
            return false;
        }

        return true;
    }
}