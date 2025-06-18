using Marshal.Api;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LinuxDedicatedServer.Api;

public interface IConsoleTunnelManager
{
    public Task<bool> TryStartTunnel(InstanceProcess instance);
    public Task<bool> TryStopTunnel(InstanceProcess instance);
    public Task<bool> TryStopTunnel(string instanceName);
    public Task ForceStopAll();
    public bool ExistingTunnel(InstanceProcess instance);
    public bool ExistingTunnel(string instanceName);
    public IEnumerable<string> GetOpenTunnels();
}

public class ConsoleTunnelManagerConfig
{
    // These two port indicate how many tunnels can be open at the same time
    public int StartingPort { get; init; }
    public int EndingPort { get; init; }
}

public class ConsoleTunnelManager(ConsoleTunnelManagerConfig config, ILogger<ConsoleTunnelManager> logger, IProfiler<ConsoleTunnelManager> profiler) : IConsoleTunnelManager, IAsyncDisposable
{
    private readonly Dictionary<string, int> ServiceMapping = [];
    private TunnelServerService?[] Services { get; } = BuildServiceArray(config);

    public async Task<bool> TryStartTunnel(InstanceProcess instance)
    {
        for (int i = 0; i < Services.Length; i++)
        {
            if (Services[i] is not null)
            {
                continue;
            }

            var tunnelConfig = new TunnelServiceServiceConfig
            {
                ListenerPort = config.StartingPort + i * 2,
                BroadcastPort = config.EndingPort + i * 2 + 1,
            };
            Services[i] = new TunnelServerService(instance.Process, tunnelConfig);

            try
            {
                using var scope = profiler.Profile("Start tunnel service");
                await Services[i]!.StartAsync(instance.CancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start tunnel service for instance '{name}'", instance.Metadata.Name);
                Services[i] = null;

                return false;
            }

            ServiceMapping.Add(instance.Metadata.Name, i);

            logger.LogInformation("Started tunnel service for instance '{name}' on ports ({lPort}, {bPort})", instance.Metadata.Name, tunnelConfig.ListenerPort, tunnelConfig.BroadcastPort);
            return true;
        }

        logger.LogWarning("Failed to start tunnel service for instance '{name}' - not enough ports free", instance.Metadata.Name);
        return false;
    }

    public Task<bool> TryStopTunnel(InstanceProcess instance) => TryStopTunnel(instance.Metadata.Name);

    public async Task<bool> TryStopTunnel(string instanceName)
    {
        if (!ServiceMapping.TryGetValue(instanceName, out var index))
        {
            return false;
        }

        var instance = Services[index];

        if (instance is null)
        {
            logger.LogWarning("Tunnel instance '{name}' was already stopped or is unavailable.", instanceName);
            return true; // ?? well... its technically true
        }

        try
        {
            using var scope = profiler.Profile("Stop tunnel service");
            await instance.StopAsync(default);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to stop tunnel service '{name}'.", instanceName);
            return false;
        }

        Services[index] = null;
        ServiceMapping.Remove(instanceName);

        return true;
    }

    public async Task ForceStopAll()
    {
        await Task.WhenAll(Services.Where(x => x is not null).Select(x => x!.StopAsync(default)));
    }

    public bool ExistingTunnel(InstanceProcess instance) => ExistingTunnel(instance.Metadata.Name);

    public bool ExistingTunnel(string instanceName)
    {
        if (!ServiceMapping.TryGetValue(instanceName, out var index))
        {
            return false;
        }

        return Services[index] is not null;
    }

    public IEnumerable<string> GetOpenTunnels()
    {
        foreach (var item in ServiceMapping)
        {
            if (Services is null)
            {
                ServiceMapping.Remove(item.Key);
            }
            else
            {
                yield return item.Key;
            }
        }
    }

    private static TunnelServerService?[] BuildServiceArray(ConsoleTunnelManagerConfig config)
    {
        return new TunnelServerService?[(int)Math.Floor((double)(config.EndingPort - config.StartingPort) / 2)];
    }

    public async ValueTask DisposeAsync()
    {
        await ForceStopAll();
    }
}

public class TunnelServiceServiceConfig
{
    public required int BroadcastPort { get; init; }
    public required int ListenerPort { get; init; }
}

public class TunnelServerService(Process process, TunnelServiceServiceConfig config) : IHostedService, IDisposable
{
    private TunnelInputService? _inputService;
    private TunnelOutputService? _outputService;

    public bool Active { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (Active)
        {
            return;
        }

        _inputService = new TunnelInputService(process, config.ListenerPort);
        _outputService = new TunnelOutputService(process, config.BroadcastPort);

        await _inputService.StartAsync(cancellationToken);
        await _outputService.StartAsync(cancellationToken);
        Active = true;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!Active)
        {
            return;
        }

        await _inputService!.StopAsync(cancellationToken);
        await _outputService!.StopAsync(cancellationToken);
        Active = false;
    }

    public void Dispose()
    {
        _inputService?.Dispose();
        _outputService?.Dispose();
    }
}

public class TunnelInputService(Process process, int port) : IHostedService, IDisposable
{
    private CancellationTokenSource? _tokenSource;
    private TcpListener? _tcpListener;
    private Task? _backgroundTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_tokenSource is not null)
        {
            return Task.CompletedTask;
        }

        _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _tcpListener = new TcpListener(IPAddress.Any, port);
        _tcpListener.Start();

        _backgroundTask = Task.Run(() => ProcessLoopAsync(_tokenSource.Token), _tokenSource.Token);

        return Task.CompletedTask;
    }

    private async Task ProcessLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await _tcpListener!.AcceptTcpClientAsync(cancellationToken);
                _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);

        try
        {
            while (!cancellationToken.IsCancellationRequested && !reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(cancellationToken);

                if (line is null)
                {
                    break;
                }

                if (!process.HasExited && process.StandardInput.BaseStream.CanWrite)
                {
                    await process.StandardInput.WriteLineAsync(line);
                    await process.StandardInput.FlushAsync(cancellationToken);
                }
            }
        }
        finally
        {
            client.Dispose();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_tokenSource is null)
        {
            return;
        }

        _tokenSource.Cancel();
        _tcpListener?.Stop();

        if (_backgroundTask is not null)
        {
            await _backgroundTask;
        }

        _tokenSource.Dispose();
        _tcpListener?.Dispose();
        _backgroundTask?.Dispose();

        _tokenSource = null;
        _tcpListener = null;
        _backgroundTask = null;
    }

    public void Dispose()
    {
        _ = StopAsync(default);
    }
}

/// <summary>
/// Service that broadcasts a process' output. 
/// </summary>
public class TunnelOutputService(Process process, int port) : IHostedService, IDisposable
{
    private CancellationTokenSource? _tokenSource;
    private UdpClient? _udpClient;
    private Task? _backgroundTask;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_tokenSource is not null)
        {
            return Task.CompletedTask;
        }

        _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _udpClient = new UdpClient(port);
        _udpClient.EnableBroadcast = true;
        _backgroundTask = Task.Run(() => ProcessLoopAsync(_tokenSource.Token), _tokenSource.Token);

        return Task.CompletedTask;
    }

    private async Task ProcessLoopAsync(CancellationToken cancellationToken)
    {
        while (!process.StandardOutput.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await process.StandardOutput.ReadLineAsync(cancellationToken);

            if (line is null)
            {
                break;
            }

            var bytes = Encoding.UTF8.GetBytes(line);
            await _udpClient!.SendAsync(bytes, bytes.Length, new System.Net.IPEndPoint(IPAddress.Broadcast, port));
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_tokenSource is null)
        {
            return;
        }

        _tokenSource.Cancel();

        if (_backgroundTask is not null)
            await _backgroundTask;

        _udpClient?.Dispose();
        _tokenSource.Dispose();
        _backgroundTask?.Dispose();

        _tokenSource = null;
        _udpClient = null;
        _backgroundTask = null;
    }

    public void Dispose()
    {
        _ = StopAsync(default);
    }
}
