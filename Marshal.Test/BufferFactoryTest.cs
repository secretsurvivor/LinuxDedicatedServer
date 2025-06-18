using LinuxDedicatedServer.Api.Buffer.v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinuxDedicatedServer.Test;

public class BufferFactoryTest
{
    [Fact]
    public async Task ReadType_ShouldReturnCorrectValue()
    {
        using var stream = new MemoryStream();
        await BufferFactory.WriteType(stream, 42);
        stream.Position = 0;
        int result = await BufferFactory.ReadType<int>(stream);
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task WriteType_ShouldWriteCorrectValue()
    {
        using var stream = new MemoryStream();
        await BufferFactory.WriteType(stream, 100);
        stream.Position = 0;
        byte[] buffer = new byte[sizeof(int)];
        await stream.ReadAsync(buffer, 0, buffer.Length);
        Assert.NotEmpty(buffer);
    }

    [Fact]
    public async Task ReadTuple_ShouldReturnCorrectTuple()
    {
        using var stream = new MemoryStream();
        var tuple = (1, 1.0d);

        await BufferFactory.WriteTuple(stream, tuple);

        stream.Position = 0;
        var result = await BufferFactory.ReadTuple<(int, double)>(stream);

        Assert.Equal(tuple, result);
    }

    [Fact]
    public async Task WriteTuple_ShouldWriteTupleCorrectly()
    {
        using var stream = new MemoryStream();
        var tuple = (5, 7.0d);
        await BufferFactory.WriteTuple(stream, tuple);
        stream.Position = 0;
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public async Task ReadStruct_ShouldReturnCorrectStruct()
    {
        using var stream = new MemoryStream();
        var data = new TestStruct { A = 1, B = 2.5f };

        await BufferFactory.WriteStruct(stream, data);

        stream.Position = 0;
        var result = await BufferFactory.ReadStruct<TestStruct>(stream);
        Assert.Equal(data, result);
    }

    [Fact]
    public async Task WriteStruct_ShouldWriteCorrectStruct()
    {
        using var stream = new MemoryStream();
        var data = new TestStruct { A = 10, B = 3.14f };
        await BufferFactory.WriteStruct(stream, data);
        stream.Position = 0;
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void AddType_ShouldIncreaseBufferSize()
    {
        var factory = new BufferFactory();
        factory.AddType<int>();
        Assert.True(factory.BufferSize == sizeof(int));
    }

    [Fact]
    public void AddTuple_ShouldIncreaseBufferSize()
    {
        var factory = new BufferFactory();
        factory.AddTuple<(int, float)>();
        Assert.True(factory.BufferSize >= sizeof(int) + sizeof(float));
    }

    [Fact]
    public void AddStruct_ShouldIncreaseBufferSize()
    {
        var factory = new BufferFactory();
        factory.AddStruct<TestStruct>();
        Assert.True(factory.BufferSize >= sizeof(int) + sizeof(float));
    }

    [Fact]
    public async Task ReadClass_ShouldReturnCorrectClass()
    {
        using var stream = new MemoryStream();
        var data = new TestClass
        {
            A = 1,
            B = 5.2F,
        };

        await BufferFactory.WriteClass(stream, data);

        stream.Position = 0;
        var result = await BufferFactory.ReadClass<TestClass>(stream);

        Assert.Equal(data.A, result.A);
        Assert.Equal(data.B, result.B);
    }

    [Fact]
    public async Task Resolver_ShouldResolveCustomResolver()
    {
        using var stream = new MemoryStream();
        var data = DateTime.UtcNow;

        await BufferFactory.WriteType(stream, data);

        stream.Position = 0;
        var result = await BufferFactory.ReadType<DateTime>(stream);

        Assert.Equal(data, result);
    }
}

public struct TestStruct
{
    public int A;
    public float B;
}

public class TestClass
{
    public int A { get; set; }
    public float B { get; set; }
}
