using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LinuxDedicatedServer.Api.Buffer.v1;

public class BufferWriter(Memory<byte> buffer)
{
    private readonly Memory<byte> _buffer = buffer;
    private int _position = 0;

    public BufferWriter WriteBytes(ReadOnlySpan<byte> bytes)
    {
        if (_position + bytes.Length > _buffer.Length)
        {
            throw new ArgumentException("Attempted to write beyond buffer length", nameof(bytes));
        }

        bytes.CopyTo(_buffer.Span.Slice(_position));
        _position += bytes.Length;

        return this;
    }

    public BufferWriter Write<T>(T value)
    {
        if (BufferTypeResolver.ValidateAndResolve<T>(out var resolver))
        {
            resolver.Write(this, value);
            return this;
        }

        var size = Marshal.SizeOf<T>();

        if (_position + size > _buffer.Length)
        {
            throw new ArgumentException("Attempted to write beyond buffer length", nameof(T));
        }

        var slice = _buffer.Span.Slice(_position, size);
        _position += size;

        PrimitiveWrite(value, slice);

        return this;
    }

    public void ReflectionWrite(Type type, object? value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value), "Values written to the buffer cannot be null");
        }

        var method = typeof(BufferWriter).GetMethod(nameof(Write))!.MakeGenericMethod(type);
        method.Invoke(this, [value]);
    }

    private static void PrimitiveWrite<T>(T value, Span<byte> destination)
    {
        switch (value)
        {
            case bool b:
                BitConverter.TryWriteBytes(destination, b);
                break;
            case byte b:
                destination[0] = b;
                break;
            case sbyte sb:
                destination[0] = (byte)sb;
                break;
            case char c:
                BitConverter.TryWriteBytes(destination, c);
                break;
            case short s:
                BitConverter.TryWriteBytes(destination, s);
                break;
            case ushort us:
                BitConverter.TryWriteBytes(destination, us);
                break;
            case int i:
                BitConverter.TryWriteBytes(destination, i);
                break;
            case uint ui:
                BitConverter.TryWriteBytes(destination, ui);
                break;
            case long l:
                BitConverter.TryWriteBytes(destination, l);
                break;
            case ulong ul:
                BitConverter.TryWriteBytes(destination, ul);
                break;
            case float f:
                BitConverter.TryWriteBytes(destination, f);
                break;
            case double d:
                BitConverter.TryWriteBytes(destination, d);
                break;
            default:
                throw new NotSupportedException($"Unsupported type: {typeof(T).Name}");
        }
    }

    public BufferWriter WriteTuple<T>(T value) where T : ITuple
    {
        var genericArguments = typeof(T).GetGenericArguments();
        var length = value.Length;

        for (int i = 0; i < length; i++)
        {
            ReflectionWrite(genericArguments[i], value[i]);
        }

        return this;
    }

    public BufferWriter WriteStruct<T>(T value) where T : struct
    {
        foreach (var field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            ReflectionWrite(field.FieldType, field.GetValueDirect(__makeref(value)));
        }

        return this;
    }

    public BufferWriter WriteClass<T>(T value) where T : class
    {
        foreach (var property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            ReflectionWrite(property.PropertyType, property.GetValue(value));
        }

        return this;
    }
}
