using System.Reflection;
using System.Runtime.CompilerServices;

namespace LinuxDedicatedServer.Api.Buffer.v1;

public class ManagedBufferWriter
{
    private readonly ManagedFactory _factory = new ManagedFactory();
    private readonly IList<object> _values = [];
    private readonly IList<int> _lengths = [];

    public static async Task WriteTuple<T>(Stream stream, T value) where T : ITuple
    {
        var writer = new ManagedBufferWriter();
        Type[] types = typeof(T).GetGenericArguments();

        for (int i = 0; i < value.Length; i++)
        {
            writer.Write(types[i], value[i]);
        }

        await writer.FlushStream(stream);
    }

    public static async Task WriteStruct<T>(Stream stream, T value) where T : struct
    {
        var writer = new ManagedBufferWriter();

        foreach (var field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            writer.Write(field.FieldType, field.GetValueDirect(__makeref(value)));
        }

        await writer.FlushStream(stream);
    }

    public static async Task WriteClass<T>(Stream stream, T value) where T : class
    {
        var writer = new ManagedBufferWriter();

        foreach (var property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            writer.Write(property.PropertyType, property.GetValue(value));
        }

        await writer.FlushStream(stream);
    }

    public ManagedBufferWriter Write(Type type, object? value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        _factory.AddType(type);

        if (ManagedTypeResolver.Resolve(type, out var resolver))
        {
            (object convertValue, int length) = resolver.ConvertValue(value);

            _values.Add(convertValue);
            _lengths.Add(length);

            return this;
        }

        _values.Add(value);

        return this;
    }

    public ManagedBufferWriter Write<T>(T value)
    {
        return Write(typeof(T), value);
    }

    private BufferFactory BuildBody()
    {
        var factory = new BufferFactory();

        for (int i = 0; i < _values.Count; i++)
        {
            var type = _factory.BufferTypes[i];

            if (ManagedTypeResolver.Resolve(type, out var resolver))
            {
                resolver.WriteAddValue(factory, _factory.BufferTypes[i], _values[i]);
            }
            else
            {
                factory.AddType(type);
            }
        }

        return factory;
    }

    public async Task FlushStream(Stream stream)
    {
        await _factory.BuildHeader().WriteAsync(stream, writer =>
        {
            foreach (var length in _lengths)
            {
                writer.Write(length);
            }
        });

        await BuildBody().WriteAsync(stream, writer =>
        {
            for (int i = 0; i < _values.Count; i++)
            {
                var type = _factory.BufferTypes[i];

                if (ManagedTypeResolver.Resolve(type, out var resolver))
                {
                    resolver.WriteValue(writer, _factory.BufferTypes[i], _values[i]);
                }
                else
                {
                    writer.ReflectionWrite(type, _values[i]);
                }
            }
        });
    }
}
