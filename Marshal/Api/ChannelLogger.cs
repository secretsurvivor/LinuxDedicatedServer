using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Threading.Channels;

namespace LinuxDedicatedServer.Api;

public class ChannelLogger(string categoryName, Channel<ChannelLogMessage> channel) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        channel.Writer.TryWrite(new ChannelLogMessage
        {
            Timestamp = DateTime.UtcNow,
            Category = categoryName,
            Level = logLevel,
            Message = formatter(state, exception),
        });
    }
}

public class ChanelLoggerProvider(Channel<ChannelLogMessage> channel) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new ChannelLogger(categoryName, channel);

    public void Dispose() { }
}

public static class ChannelLoggerLoggingBuilder
{
    public static ILoggingBuilder AddChanelLogger(this ILoggingBuilder builder, ChannelLoggerConfig config)
    {
        builder.Services.AddSingleton(_ => Channel.CreateUnbounded<ChannelLogMessage>(new UnboundedChannelOptions { SingleReader = true, AllowSynchronousContinuations = false }));
        builder.Services.AddSingleton<ILoggerProvider, ChanelLoggerProvider>();
        builder.Services.AddSingleton(config);
        builder.Services.AddHostedService<ChannelLoggerService>();

        return builder;
    }
}

public class ChannelLoggerConfig
{
    public required string OutputPath { get; init; }
}

public class ChannelLoggerService(ChannelLoggerConfig config, Channel<ChannelLogMessage> channel) : BackgroundService
{
    private readonly SlidingFileStreamProvider _streamProvider = new SlidingFileStreamProvider(TimeSpan.FromSeconds(30));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var file = GetStream();
        using var writer = new StreamWriter(file.FileStream, Encoding.UTF8, leaveOpen: true);

        while (await channel.Reader.WaitToReadAsync(stoppingToken))
        {
            await foreach (var message in channel.Reader.ReadAllAsync(stoppingToken))
            {
                await writer.WriteAsync($"\n[{message.Timestamp:T} {Enum.GetName(message.Level)}] {message.Category}: {message.Message}");
            }

            await writer.FlushAsync();
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _streamProvider.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }

    private ISlidingFileStream GetStream()
    {
        var path = Path.Combine(config.OutputPath, $"{DateTime.Today:dd-MM-yy}.log.txt");

        if (File.Exists(path))
        {
            return _streamProvider.GetStream(path, FileMode.Append, FileAccess.Write);
        }
        else
        {
            return _streamProvider.GetStream(path, FileMode.Create, FileAccess.Write);
        }
    }
}

public record ChannelLogMessage
{
    public required DateTime Timestamp { get; init; }
    public required string Category { get; init; }
    public required LogLevel Level { get; init; }
    public required string Message { get; init; }
}