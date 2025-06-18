using System.Text;

namespace LinuxDedicatedServer.Api.Buffer.v1;

public static class BufferFactoryHelper
{
    public static async Task WriteString(Stream stream, string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        var factory = new BufferFactory().AddType<int>();

        for (int i = 0; i < bytes.Length; i++)
        {
            factory.AddType<byte>();
        }

        await factory.WriteAsync(stream, writer =>
        {
            writer.Write(bytes.Length);
            writer.WriteBytes(bytes);
        });
    }

    public static async Task<string> ReadString(Stream stream, int length)
    {
        var factory = new BufferFactory();

        for (int i = 0; i < length; i++)
        {
            factory.AddType<byte>();
        }

        var bytes = (await factory.ReadAsync(stream)).ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }

    public static async Task<string> ReadString(Stream stream)
    {
        var factory = new BufferFactory().AddType<int>();
        var length = (await factory.ReadAsync(stream)).Read<int>();

        return await ReadString(stream, length);
    }
}
