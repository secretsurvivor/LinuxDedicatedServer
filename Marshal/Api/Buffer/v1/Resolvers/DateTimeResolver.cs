using LinuxDedicatedServer.Api.Buffer.v1;
using System.Runtime.InteropServices;

namespace LinuxDedicatedServer.Api.Buffer.v1.Resolvers;

public class DateTimeResolver : IBufferTypeResolver<DateTime>
{
    public int GetSize()
    {
        return Marshal.SizeOf<long>() + Marshal.SizeOf<int>();
    }

    public DateTime Read(BufferReader reader)
    {
        long ticks = reader.Read<long>();
        int kind = reader.Read<int>();

        return new DateTime(ticks, (DateTimeKind)kind);
    }

    public void Write(BufferWriter writer, DateTime value)
    {
        writer.Write(value.Ticks)
              .Write((int)value.Kind);
    }
}
