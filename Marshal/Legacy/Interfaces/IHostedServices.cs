namespace LinuxDedicatedServer.Legacy.Interfaces
{
    public interface IHostedServices
    {
        public IConfig Config { get; }
        public ILoggerHandler Logger { get; }
    }
}
