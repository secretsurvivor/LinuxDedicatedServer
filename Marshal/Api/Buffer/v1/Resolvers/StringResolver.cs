using LinuxDedicatedServer.Api.Buffer.v1;
using System.Text;

namespace LinuxDedicatedServer.Api.Buffer.v1.Resolvers;

public class StringResolver : IManagedTypeResolver
{
    public (object value, int length) ConvertValue(object value)
    {
        Memory<byte> bytes = Encoding.UTF8.GetBytes((string)value);

        return (bytes, bytes.Length);
    }

    public void WriteAddValue(BufferFactory factory, Type type, object value)
    {
        Memory<byte> bytes = (Memory<byte>)value;

        for (int i = 0; i < bytes.Length; i++)
        {
            factory.AddType<byte>();
        }
    }

    public void WriteValue(BufferWriter writer, Type type, object value)
    {
        Memory<byte> bytes = (Memory<byte>)value;
        writer.WriteBytes(bytes.Span);
    }

    public void ReadAddValue(BufferFactory factory, Type type, int length)
    {
        for (int i = 0; i < length; i++)
        {
            factory.AddType<byte>();
        }
    }

    public object ReadValue(BufferReader reader, Type type, int length)
    {
        ReadOnlySpan<byte> bytes = reader.ReadBytes(length);

        return Encoding.UTF8.GetString(bytes);
    }
}
