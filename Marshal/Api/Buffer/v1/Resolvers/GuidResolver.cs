using LinuxDedicatedServer.Api.Buffer.v1;

namespace LinuxDedicatedServer.Api.Buffer.v1.Resolvers;

public class GuidResolver : IBufferTypeResolver<Guid>
{
    private const int GuidLength = 16;

    public int GetSize()
    {
        return GuidLength;
    }

    public Guid Read(BufferReader reader)
    {
        var bytes = reader.ReadBytes(GuidLength);
        return new Guid(bytes);
    }

    public void Write(BufferWriter writer, Guid value)
    {
        writer.WriteBytes(value.ToByteArray());
    }
}
