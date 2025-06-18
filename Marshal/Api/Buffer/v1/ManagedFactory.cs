namespace LinuxDedicatedServer.Api.Buffer.v1;

public class ManagedFactory
{
    public List<Type> BufferTypes { get; } = [];
    public int HeaderLength { get; private set; } = 0;

    private static bool IsSupported(Type type)
    {
        return ManagedTypeResolver.ResolverExists(type) || BufferTypeResolver.IsSupportedType(type);
    }

    public Type AddType(Type type)
    {
        if (!IsSupported(type))
        {
            throw new NotSupportedException($"Type '{type.Name}' is not supported");
        }

        BufferTypes.Add(type);

        return type;
    }

    public Type AddType<T>()
    {
        return AddType(typeof(T));
    }

    public BufferFactory BuildHeader()
    {
        var factory = new BufferFactory();

        foreach (var type in BufferTypes)
        {
            if (ManagedTypeResolver.ResolverExists(type))
            {
                factory.AddType<int>();
                HeaderLength++;
            }
        }

        return factory;
    }
}