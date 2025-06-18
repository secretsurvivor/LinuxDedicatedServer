using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Reflection;

namespace LinuxDedicatedServer.Api.Buffer.v1;

public class BufferFactory
{
    private int _bufferSize;

    public int BufferSize { get => _bufferSize; }

    public static async Task<T> ReadType<T>(Stream stream)
    {
        return (await new BufferFactory().AddType<T>().ReadAsync(stream)).Read<T>();
    }

    public static async Task WriteType<T>(Stream stream, T value)
    {
        await new BufferFactory().AddType<T>().WriteAsync(stream, x => x.Write(value));
    }

    public static async Task<T> ReadTuple<T>(Stream stream) where T : ITuple
    {
        return (await new BufferFactory().AddTuple<T>().ReadAsync(stream)).ReadTuple<T>();
    }

    public static async Task WriteTuple<T>(Stream stream, T value) where T : ITuple
    {
        await new BufferFactory().AddTuple<T>().WriteAsync(stream, x => x.WriteTuple(value));
    }

    public static async Task<T> ReadStruct<T>(Stream stream) where T : struct
    {
        return (await new BufferFactory().AddStruct<T>().ReadAsync(stream)).ReadStruct<T>();
    }

    public static async Task WriteStruct<T>(Stream stream, T value) where T : struct
    {
        await new BufferFactory().AddStruct<T>().WriteAsync(stream, x => x.WriteStruct(value));
    }

    public static async Task<T> ReadClass<T>(Stream stream) where T : class
    {
        return (await new BufferFactory().AddClass<T>().ReadAsync(stream)).ReadClass<T>();
    }

    public static async Task WriteClass<T>(Stream stream, T value) where T : class
    {
        await new BufferFactory().AddClass<T>().WriteAsync(stream, writer => writer.WriteClass(value));
    }

    public BufferFactory AddType<T>()
    {
        if (BufferTypeResolver.ValidateAndResolve<T>(out var resolver))
        {
            _bufferSize += resolver.GetSize();

            return this;
        }

        _bufferSize += Marshal.SizeOf<T>();

        return this;
    }

    public BufferFactory AddType(Type type)
    {
        if (BufferTypeResolver.ValidateAndResolve(type, out var resolver))
        {
            _bufferSize += resolver.GetSize();

            return this;
        }

        _bufferSize += Marshal.SizeOf(type);

        return this;
    }

    public BufferFactory AddTuple<T>() where T : ITuple
    {
        foreach (var type in typeof(T).GetGenericArguments())
        {
            AddType(type);
        }

        return this;
    }

    public BufferFactory AddStruct<T>() where T : struct
    {
        foreach (var field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            AddType(field.FieldType);
        }

        return this;
    }

    public BufferFactory AddClass<T>() where T : class
    {
        foreach (var property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            AddType(property.PropertyType);
        }

        return this;
    }

    public async Task<BufferReader> ReadAsync(Stream stream)
    {
        Memory<byte> buffer = new byte[_bufferSize];
        await stream.ReadExactlyAsync(buffer);

        return new BufferReader(buffer);
    }

    public async Task WriteAsync(Stream stream, Action<BufferWriter> action)
    {
        Memory<byte> buffer = new byte[_bufferSize];
        action(new BufferWriter(buffer));

        await stream.WriteAsync(buffer);
        await stream.FlushAsync();
    }
}