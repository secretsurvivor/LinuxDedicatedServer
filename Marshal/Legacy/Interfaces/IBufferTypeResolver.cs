using LinuxDedicatedServer.Api.Buffer.v1;

namespace LinuxDedicatedServer.Legacy.Interfaces;

public interface IBufferResolver
{
    public int GetSize();
}

public interface IBufferTypeResolver<T> : IBufferResolver
{
    public void Write(BufferWriter writer, T value);
    public T Read(BufferReader reader);
}
