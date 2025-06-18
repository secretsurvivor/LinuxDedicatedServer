namespace LinuxDedicatedServer.Api;

public interface ISlidingFileStreamProvider : IDisposable, IAsyncDisposable
{
    public ISlidingFileStream GetStream(string filePath, FileMode mode, FileAccess access);
}

public interface ISlidingFileStream : IDisposable
{
    public FileStream FileStream { get; }
}

public class SlidingFileStreamProvider(TimeSpan expiration) : ISlidingFileStreamProvider
{
    private (string filePath, FileMode mode, FileAccess access)? _lastOptions;
    private Timer? _timer;
    private readonly Lock _lock = new Lock();

    public FileStream? Stream { get; private set; }

    public ISlidingFileStream GetStream(string filePath, FileMode mode, FileAccess access)
    {
        lock (_lock)
        {
            if (_lastOptions is not null)
            {
                var (lastPath, lastMode, lastAccess) = _lastOptions.Value;
                if (lastPath != filePath || lastMode != mode || lastAccess != access)
                {
                    InternalDispose();
                }
            }

            _lastOptions = (filePath, mode, access);
            Stream ??= new FileStream(filePath, mode, access);
            _timer?.Dispose();

            return new SlidingFileStreamImplementation(this);
        }
    }

    public void ResetTimer()
    {
        _timer?.Dispose();
        _timer = new Timer(_ =>
        {
            lock (_lock)
            {
                InternalDispose();
            }
        }, null, expiration, Timeout.InfiniteTimeSpan);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            InternalDispose();
        }
    }

    private void InternalDispose()
    {
        Stream?.Dispose();
        _timer?.Dispose();

        Stream = null;
        _timer = null;
        _lastOptions = null;
    }

    public async ValueTask DisposeAsync()
    {
        lock (_lock)
        {
            _lastOptions = null;
        }

        if (Stream is not null)
        {
            await Stream.DisposeAsync();
        }

        if (_timer is not null)
        {
            await _timer.DisposeAsync();
        }

        Stream = null;
        _timer = null;
    }
}

public class SlidingFileStreamImplementation(SlidingFileStreamProvider parent) : ISlidingFileStream
{
    public FileStream FileStream { get => parent.Stream!; }

    public void Dispose()
    {
        parent.ResetTimer();
    }
}