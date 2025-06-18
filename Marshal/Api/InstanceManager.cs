using Marshal.Api;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace LinuxDedicatedServer.Api;

public interface IInstanceManager
{

}

public class InstanceManagerConfig
{
    public required string InstancePath { get; set; }
}

public class InstanceManager(InstanceManagerConfig config, ILogger<InstanceManager> logger, IProfiler<InstanceManager> profiler) : IAsyncDisposable
{
    private ConcurrentDictionary<string, (InstanceProcess process, IInstanceService service)> ActiveInstances { get; } = [];

    public async Task<bool> StartInstance(InstanceMetadata data)
    {
        if (!Directory.Exists(config.InstancePath))
        {
            Directory.CreateDirectory(config.InstancePath);
        }

        if (!Directory.Exists(Path.Combine(config.InstancePath, data.FolderName)))
        {
            return false;
        }
    }

    public Task StopInstance(string instanceName)
    {

    }

    public bool TryGetInstanceProcess(string instanceName, out InstanceProcess instanceProcess)
    {
        if (ActiveInstances.TryGetValue(instanceName, out var instance))
        {
            instanceProcess = instance.process;
            return true;
        }

        instanceProcess = default;
        return false;
    }

    public bool TryGetInstanceMetadata(string instanceName, out InstanceMetadata instanceMetadata)
    {
        if (ActiveInstances.TryGetValue(instanceName, out var instance))
        {
            instanceMetadata = instance.process.Metadata;
            return true;
        }

        instanceMetadata = default!;
        return false;
    }

    public IEnumerable<InstanceMetadata> GetActiveInstances()
    {
        foreach (var (process, _) in ActiveInstances.Values)
        {
            yield return process.Metadata;
        }
    }

    public async Task ForceStopAll()
    {
        await Task.WhenAll(ActiveInstances.Values.Select(x => x.service.StopAsync()));
    }

    public async ValueTask DisposeAsync()
    {
        await ForceStopAll();
    }
}

public interface IInstanceService
{
    public Task<InstanceProcess> StartAsync(CancellationToken cancellationToken = default);
    public Task StopAsync(CancellationToken cancelToken = default);
}

public class InstanceMetadata
{
    public required string Name { get; init; }
    public string FolderName { get => Name.ToLower(); }
    public required string Version { get; init; }
    public required string PathToExecutable { get; init; } // relational
    public required DependencyList Dependencies { get; init; }
    public required IEnumerable<KeyValuePair<string, string>> Arguments { get; init; }
}

public readonly struct InstanceProcess
{
    public required InstanceMetadata Metadata { get; init; }
    public required Process Process { get; init; }
    public required CancellationToken CancellationToken { get; init; }
}

public class InstanceService(InstanceManagerConfig config, InstanceMetadata metadata) : IInstanceService
{
    private CancellationTokenSource? _tokenSource;
    private Process? _process;

    public async Task<InstanceProcess> StartAsync(CancellationToken cancellationToken = default)
    {
        if (_process is not null && !_process.HasExited)
        {
            throw new InvalidOperationException();
        }

        if (!await metadata.Dependencies.IsInstalled())
        {
            
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(config.InstancePath, metadata.FolderName, metadata.PathToExecutable),
            Arguments = string.Join(" ", metadata.Arguments.Select(x => $"{x.Key}={x.Value}")),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true,
        };

        _process = Process.Start(startInfo);
        _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (_process is null)
        {
            throw new InvalidOperationException();
        }

        return new InstanceProcess { Metadata = metadata, Process = _process, CancellationToken = _tokenSource.Token };
    }

    public async Task StopAsync(CancellationToken cancelToken = default)
    {
        if (_process is null || _process.HasExited)
        {
            return;
        }

        _tokenSource?.Cancel();
        _process.Kill();
        await _process.WaitForExitAsync(cancelToken);

        _process.Dispose();
        _process = null;
    }
}