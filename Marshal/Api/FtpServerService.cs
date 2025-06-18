using FubarDev.FtpServer;
using FubarDev.FtpServer.FileSystem.DotNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LinuxDedicatedServer.Api;

public interface IFtpService : IDisposable
{
    public Task StartAsync(CancellationToken cancellationToken = default);
    public Task StopAsync(CancellationToken cancellationToken = default);
}

public class FtpServerServiceConfig
{
    public required string DirectoryPath { get; init; }
    public int Port { get; init; } = 2121;
}

public class FtpServerService(FtpServerServiceConfig config) : IFtpService
{
    private IFtpServerHost? _serverHost;
    private IHost? _host;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(config.DirectoryPath))
        {
            Directory.CreateDirectory(config.DirectoryPath);
        }

        _host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services.Configure<DotNetFileSystemOptions>(options =>
                {
                    options.RootPath = config.DirectoryPath;
                });
                services.AddFtpServer(builder => builder
                    .UseDotNetFileSystem()
                    .EnableAnonymousAuthentication());
                services.Configure<FtpServerOptions>(opt =>
                {
                    opt.Port = config.Port;
                    opt.ServerAddress = "0.0.0.0";
                });
            })
            .Build();

        await _host.StartAsync(cancellationToken);

        _serverHost = _host.Services.GetRequiredService<IFtpServerHost>();
        await _serverHost.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_serverHost is not null)
        {
            await _serverHost.StopAsync(cancellationToken);
        }

        if (_host is not null)
        {
            await _host.StopAsync(cancellationToken);
        }
    }

    public void Dispose()
    {
        StopAsync().Wait();
        _host?.Dispose();
        GC.SuppressFinalize(this);
    }
}
