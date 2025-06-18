namespace LinuxDedicatedServer.Api;

public static class Momad
{
    public static T ReturnAndDispose<T>(T value, IDisposable disposable)
    {
        try
        {
            return value;
        }
        finally
        {
            disposable.Dispose();
        }
    }

    public static async Task<T> ReturnAndDisposeAsync<T>(T value, IAsyncDisposable disposable)
    {
        try
        {
            return value;
        }
        finally
        {
            await disposable.DisposeAsync();
        }
    }

    public static async Task<T> ReturnAndDisposeAsync<T>(Task<T> value, IDisposable disposable)
    {
        try
        {
            return await value;
        }
        finally
        {
            disposable.Dispose();
        }
    }

    public static async Task<T> ReturnAndDisposeAsync<T>(Task<T> value, IAsyncDisposable disposable)
    {
        try
        {
            return await value;
        }
        finally
        {
            await disposable.DisposeAsync();
        }
    }
}
