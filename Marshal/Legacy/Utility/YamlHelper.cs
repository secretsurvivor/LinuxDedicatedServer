using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LinuxDedicatedServer.Legacy.Utility
{
    [RequiresDynamicCode("Calls YamlDotNet.Serialization.DeserializerBuilder.DeserializerBuilder()")]
    public static class YamlHelper
    {
        private readonly static IDeserializer _deserializer;
        private readonly static ISerializer _serializer;

        static YamlHelper()
        {
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            _serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
        }

        public static async Task<T> DeserializeAsync<T>(StreamReader reader)
        {
            return await Task.Run(() => _deserializer.Deserialize<T>(reader));
        }

        public static async Task SerializeAsync(object obj, StreamWriter writer)
        {
            await Task.Run(() => _serializer.Serialize(writer, obj));
        }
    }
}
