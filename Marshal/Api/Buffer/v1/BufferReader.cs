using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LinuxDedicatedServer.Api.Buffer.v1;

public class BufferReader(Memory<byte> buffer)
{
    private readonly Memory<byte> _buffer = buffer;
    private int _position = 0;

    public ReadOnlySpan<byte> ReadBytes(int length)
    {
        if (_position + length > _buffer.Length)
        {
            throw new ArgumentException("Attempted to read beyond buffer length");
        }

        var slice = _buffer.Span.Slice(_position, length);
        _position += length;

        return slice;
    }

    public T Read<T>()
    {
        if (BufferTypeResolver.ValidateAndResolve<T>(out var resolver))
        {
            return resolver.Read(this);
        }

        var size = Marshal.SizeOf<T>();

        if (_position + size > _buffer.Length)
        {
            throw new ArgumentException("Attempted to read beyond buffer length", nameof(T));
        }

        var slice = _buffer.Span.Slice(_position, size);
        _position += size;

        return (T)PrimitiveConvert<T>(slice);
    }

    public object ReflectionRead(Type type)
    {
        // Very dirty but I need it
        var method = typeof(BufferReader).GetMethod(nameof(Read))!.MakeGenericMethod(type);
        return method.Invoke(this, null)!;
    }

    private static object PrimitiveConvert<T>(ReadOnlySpan<byte> bufferSlice)
    {
        return typeof(T) switch
        {
            Type t when t == typeof(bool) => BitConverter.ToBoolean(bufferSlice),
            Type t when t == typeof(byte) => bufferSlice[0],
            Type t when t == typeof(sbyte) => (sbyte)bufferSlice[0],
            Type t when t == typeof(char) => BitConverter.ToChar(bufferSlice),
            Type t when t == typeof(short) => BitConverter.ToInt16(bufferSlice),
            Type t when t == typeof(ushort) => BitConverter.ToUInt16(bufferSlice),
            Type t when t == typeof(int) => BitConverter.ToInt32(bufferSlice),
            Type t when t == typeof(uint) => BitConverter.ToUInt32(bufferSlice),
            Type t when t == typeof(long) => BitConverter.ToInt64(bufferSlice),
            Type t when t == typeof(ulong) => BitConverter.ToUInt64(bufferSlice),
            Type t when t == typeof(float) => BitConverter.ToSingle(bufferSlice),
            Type t when t == typeof(double) => BitConverter.ToDouble(bufferSlice),
            _ => throw new NotSupportedException($"Unsupported type: {typeof(T).Name}")
        };
    }

    public T ReadTuple<T>() where T : ITuple
    {
        var genericArguments = typeof(T).GetGenericArguments();
        object[] constructor = new object[genericArguments.Length];

        for (int i = 0; i < genericArguments.Length; i++)
        {
            constructor[i] = ReflectionRead(genericArguments[i]);
        }

        return (T)Activator.CreateInstance(typeof(T), constructor)!;
    }

    public T ReadStruct<T>() where T : struct
    {
        T result = Activator.CreateInstance<T>();

        foreach (var field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            field.SetValueDirect(__makeref(result), ReflectionRead(field.FieldType));
        }

        return result;
    }

    public T ReadClass<T>() where T : class
    {
        T result = Activator.CreateInstance<T>();

        foreach (var properties in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            properties.SetValue(result, ReflectionRead(properties.PropertyType));
        }

        return result;
    }
}
