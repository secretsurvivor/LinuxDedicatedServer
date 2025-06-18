using LinuxDedicatedServer.Legacy.Utility;

namespace LinuxDedicatedServer.Legacy.Interfaces
{
    public interface ILoggerHandler
    {
        public Task Information(string message);

        public Task Warning(string message);

        public Task Error(string message);

        public Task Exception(HostedException exception);
    }
}
