namespace LinuxDedicatedServer.Legacy.Models
{
    public record LogRecord(DateTime DateLogged, string LogType, string Message);
}
