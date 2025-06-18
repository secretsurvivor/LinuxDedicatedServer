using LinuxDedicatedServer.Api;
using LinuxDedicatedServer.Legacy.Interfaces;
using LinuxDedicatedServer.Legacy.Utility;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Sockets;

namespace LinuxDedicatedServer.Legacy.Services
{
    public class CommandListenService(ICommandDispatcher dispatcher, IConfig config, ILoggerHandler logger, MessageFactory factory) : BackgroundService
    {
        private readonly IConfig _config = config;
        private readonly ILoggerHandler _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TcpListener listener = new(IPAddress.Any, _config.ConsolePort);
            listener.Start();

            await _logger.Information("Command listening server has started");

            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken);
                var message = await factory.ReadMessage(client);

                
            }

            listener.Stop();
        }
    }
}
