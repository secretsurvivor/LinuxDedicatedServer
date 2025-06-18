using LinuxDedicatedServer.Legacy.Models;
using Microsoft.Extensions.Hosting;
using System.Threading.Channels;

namespace LinuxDedicatedServer.Legacy.Services
{
    public class LoggingService(Channel<LogRecord> channel) : BackgroundService
    {
        private readonly Channel<LogRecord> _channel = channel;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (await _channel.Reader.WaitToReadAsync(stoppingToken))
            {

            }
        }
    }
}
