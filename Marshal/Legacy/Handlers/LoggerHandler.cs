using LinuxDedicatedServer.Legacy.Interfaces;
using LinuxDedicatedServer.Legacy.Models;
using LinuxDedicatedServer.Legacy.Utility;
using System.Threading.Channels;

namespace LinuxDedicatedServer.Legacy.Handlers
{
    public class LoggerHandler(Channel<LogRecord> channel) : ILoggerHandler
    {
        private readonly Channel<LogRecord> _channel = channel;

        public async Task Information(string message)
        {
            await _channel.Writer.WriteAsync(new LogRecord(DateTime.UtcNow, "Information", message));
        }

        public async Task Warning(string message)
        {
            await _channel.Writer.WriteAsync(new LogRecord(DateTime.UtcNow, "Warning", message));
        }

        public async Task Error(string message)
        {
            await _channel.Writer.WriteAsync(new LogRecord(DateTime.UtcNow, "Error", message));
        }

        public async Task Exception(HostedException exception)
        {
            await _channel.Writer.WriteAsync(exception.GetLogRecord());
        }
    }
}
