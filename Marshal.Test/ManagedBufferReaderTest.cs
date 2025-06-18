using LinuxDedicatedServer.Api.Buffer.v1;

namespace LinuxDedicatedServer.Test;

public class ManagedBufferReaderTest
{
    [Fact]
    public async Task ReadTuple_ShouldReturnCorrectTuple()
    {
        using var stream = new MemoryStream();
        var tuple = (42, "hello world");

        await ManagedBufferWriter.WriteTuple(stream, tuple);

        stream.Position = 0;
        var result = await ManagedBufferReader.ReadTuple<(int, string)>(stream);

        Assert.Equal(tuple, result);
    }

    [Fact]
    public async Task ReadTuple_ShouldReturnPlainPrincible()
    {
        var stream = new MemoryStream();
        var data = (42, 2555, (long)1);

        await ManagedBufferWriter.WriteTuple(stream, data);

        stream.Position = 0;
        var result = await ManagedBufferReader.ReadTuple<(int, int, long)>(stream);

        Assert.Equal(data, result);
    }

    [Fact]
    public async Task ReadTuple_ShouldHandleEmptyTuple()
    {
        using var stream = new MemoryStream();
        var tuple = ValueTuple.Create();
        await BufferFactory.WriteTuple(stream, tuple);
        stream.Position = 0;

        var result = await ManagedBufferReader.ReadTuple<ValueTuple>(stream);

        Assert.Equal(tuple, result);
    }

    [Fact]
    public async Task ReadTuple_ShouldReadMultipleStrings()
    {
        using var stream = new MemoryStream();
        var data = ("Hello", "World", ".. anyone here?");

        await ManagedBufferWriter.WriteTuple(stream, data);

        stream.Position = 0;
        var result = await ManagedBufferReader.ReadTuple<(string, string, string)>(stream);

        Assert.Equal(data, result);
    }
}
