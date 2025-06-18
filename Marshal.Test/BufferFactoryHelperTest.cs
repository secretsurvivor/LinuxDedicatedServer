using LinuxDedicatedServer.Api.Buffer.v1;

namespace LinuxDedicatedServer.Test
{
    public class BufferFactoryHelperTest
    {
        [Fact]
        public async Task ReadString_ShouldReadString()
        {
            using var stream = new MemoryStream();
            var data = "Hello World";

            await BufferFactoryHelper.WriteString(stream, data);

            stream.Position = 0;
            var result = await BufferFactoryHelper.ReadString(stream);

            Assert.Equal(data, result);
        }
    }
}
