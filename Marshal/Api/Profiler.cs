using LinuxDedicatedServer.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Text;
using System.Threading.Channels;

namespace Marshal.Api;

public interface IProfiler<T>
{
    public IProfilerScope Profile(string @event);
}

public interface IProfilerScope : IDisposable;

public sealed class Profiler<T>(Channel<ProfilerEvent> channel) : IProfiler<T>
{
    // TODO - Allow 
    public IProfilerScope Profile(string @event)
    {
        return new ProfileScope<T>(@event, channel);
    }

    public class ProfileScope<TScope>(string @event, Channel<ProfilerEvent> channel) : IProfilerScope
    {
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();

        // Do not need to suppress GC due to it not being a normal disposible object
        public void Dispose()
        {
            stopwatch.Stop();
            channel.Writer.TryWrite(new ProfilerEvent { Timestamp = DateTime.UtcNow, Scope = typeof(TScope).FullName!, Event = @event, ElapsedTime = stopwatch.Elapsed.TotalMilliseconds });
        }
    }
}

public record ProfilerConfig
{
    public required string OutputFolder { get; init; }
}

public readonly struct ProfilerEvent
{
    public required DateTime Timestamp { get; init; }
    public required string Scope { get; init; }
    public required string Event { get; init; }
    public required double ElapsedTime { get; init; }
}

public static class ProfilerServiceCollection
{
    public static IServiceCollection AddProfiler(this IServiceCollection services, ProfilerConfig config)
    {
        services.AddSingleton(config);
        services.AddSingleton(_ => Channel.CreateUnbounded<ProfilerEvent>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false, AllowSynchronousContinuations = false }));
        services.AddSingleton(typeof(IProfiler<>), typeof(Profiler<>));
        services.AddHostedService<ProfilerChannelHandler>();
        services.AddSingleton<ProfilerReporter>();

        return services;
    }
}

public class ProfilerChannelHandler(ProfilerConfig config, Channel<ProfilerEvent> channel) : BackgroundService
{
    private readonly SlidingFileStreamProvider _streamProvider = new SlidingFileStreamProvider(TimeSpan.FromSeconds(30));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (await channel.Reader.WaitToReadAsync(stoppingToken))
        {
            var file = OpenLogFile();
            await using var writer = new ProfilerWriter(file.FileStream);

            await writer.Write(channel.Reader.ReadAllAsync(stoppingToken));
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        var file = OpenLogFile();
        await using var writer = new ProfilerWriter(file.FileStream);

        while (channel.Reader.TryRead(out var @event))
        {
            writer.Write(@event);
        }

        await base.StopAsync(cancellationToken);
    }

    private ISlidingFileStream OpenLogFile()
    {
        var path = Path.Combine(config.OutputFolder, $"profiler-{DateTime.Today:dd-MM-yy}.profile.bin");

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

public class ProfilerReporter(ProfilerConfig config)
{
    public IEnumerable<ProfilerEvent> GetAllProfileEvents()
    {
        var files = Directory.EnumerateFiles(config.OutputFolder, "*.profile.bin", new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = false });

        foreach (var file in files)
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
            using var reader = new ProfilerReader(stream);

            foreach (var @event in reader.ReadAllEvents())
            {
                yield return @event;
            }
        }
    }
}

public class ProfilerWriter(FileStream file) : IAsyncDisposable
{
    private readonly BinaryWriter writer = new BinaryWriter(file, Encoding.UTF8, leaveOpen: true); 
    
    public void Write(ProfilerEvent @event)
    {
        file.Seek(0, SeekOrigin.End);
        WriteEvent(writer, @event);
    }

    public void Write(IEnumerable<ProfilerEvent> events)
    {
        file.Seek(0, SeekOrigin.End);

        foreach (var @event in events)
            WriteEvent(writer, @event);
    }

    public async Task Write(IAsyncEnumerable<ProfilerEvent> events)
    {
        file.Seek(0, SeekOrigin.End);

        await foreach (var @event in events)
            WriteEvent(writer, @event);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return writer.DisposeAsync();
    }

    private static void WriteEvent(BinaryWriter writer, ProfilerEvent @event)
    {
        writer.Write(@event.Timestamp.Ticks);
        writer.Write(@event.Scope);
        writer.Write(@event.Event);
        writer.Write(@event.ElapsedTime);
        writer.Write((byte)0xA);
    }
}

public class ProfilerReader(FileStream file) : IDisposable
{
    private readonly BinaryReader reader = new BinaryReader(file, Encoding.UTF8, leaveOpen: true);

    public IEnumerable<ProfilerEvent> ReadAllEvents()
    {
        file.Seek(0, SeekOrigin.Begin);

        while (file.Position < file.Length)
        {
            yield return new ProfilerEvent
            {
                Timestamp = new DateTime(reader.ReadInt64()),
                Scope = reader.ReadString(),
                Event = reader.ReadString(),
                ElapsedTime = reader.ReadDouble(),
            };

            reader.ReadByte();
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        reader.Dispose();
    }
}