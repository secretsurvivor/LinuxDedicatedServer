using LinuxDedicatedServer.Api;
using LinuxDedicatedServer.Legacy.Interfaces;
using Microsoft.Extensions.Hosting;
using System.Net.Sockets;

namespace LinuxDedicatedServer.Controllers;

public class PacketRouter(IConfig config, CommandDispatcher dispatcher) : BackgroundService
{
    private TcpListener Listener;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Listener = new TcpListener(System.Net.IPAddress.Any, config.ConsolePort);
        Listener.Start();

        while (!stoppingToken.IsCancellationRequested)
        {
            var client = await Listener.AcceptTcpClientAsync(stoppingToken);

            if (client is null || !client.Connected)
            {
                continue;
            }

            using var stream = client.GetStream();

            try
            {
                if (!CommandParser.ParseCommand(stream, out Command command))
                {

                }

                var result = await dispatcher.ExecuteCommand(command);

                result.
                if (result is not null)
                {
                    await CommandWriter.WriteResultAsync(stream, result, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.WriteLine($"Error processing command: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }
    }
}
