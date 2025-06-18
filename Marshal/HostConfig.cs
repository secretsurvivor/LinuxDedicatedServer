using LinuxDedicatedServer.Legacy.Interfaces;
using LinuxDedicatedServer.Legacy.Utility;
using LinuxDedicatedServer.Legacy.Utility.Extensions;

namespace LinuxDedicatedServer
{
    public class HostConfig : IConfig
    {
        private static HostConfig? Instance { get; set; } = null;

        public static async Task<HostConfig> GetConfiguration(AppConstants constants)
        {
            if (Instance is not null)
            {
                return Instance;
            }

            using var fileRead = File.OpenRead(constants.ConfigName);

            if (fileRead is null)
            {
                var @default = GenerateDefault();

                using var fileWrite = File.OpenWrite(constants.ConfigName);

                await YamlHelper.SerializeAsync(@default, fileWrite.AsWriter());

                return Instance = @default;
            }

            var config = await YamlHelper.DeserializeAsync<HostConfig>(fileRead.AsReader());

            return Instance = config;
        }

        private static HostConfig GenerateDefault()
        {
            return new HostConfig()
            {

            };
        }

        public int ConsolePort { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string PackageDirectory { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
