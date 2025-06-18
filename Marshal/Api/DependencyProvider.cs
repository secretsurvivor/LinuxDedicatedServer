using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LinuxDedicatedServer.Api;

public interface IDependencyProvider
{
    public bool IsAvailable();
    public Task<Result> IsInstalled(Dependency dependency);
    public Task<Result<Dependency>> Install(Dependency dependency);
}

public static class DependencyProvider
{
    private static IDependencyProvider _dependencyProvider = ResolveProvider();

    public static Task<bool> IsInstalled(Dependency dependency)
    {
        return _dependencyProvider.IsInstalled(dependency).ThenAsync(() => true).GetOrDefaultAsync(false);
    }

    public static Task<Result> Install(Dependency dependency)
    {
        return _dependencyProvider.Install(dependency).StripGenericAsync();
    }

    private static IDependencyProvider ResolveProvider()
    {
        throw new NotImplementedException();
    }
}

public record Dependency
{
    public required string Name { get; init; }

    public Task<bool> IsInstalled()
    {
        return DependencyProvider.IsInstalled(this);
    }
}

public record DependencyList : IEnumerable<Dependency>
{
    public required IEnumerable<Dependency> Dependencies { get; init; }

    public async Task<bool> IsInstalled()
    {
        foreach (var dependency in Dependencies)
        {
            if (!await dependency.IsInstalled())
            {
                return false;
            }
        }

        return true;
    }

    public IEnumerator<Dependency> GetEnumerator()
    {
        return Dependencies.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

/// <summary>
/// WIP
/// </summary>
public class AptDependencyProvider : IDependencyProvider
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsAvailable()
    {
        return File.Exists("/usr/bin/apt");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task<Result<Dependency>> Install(Dependency dependency)
    {
        return await RunAptCommand($"sudo apt-get update && sudo apt-get install -y {dependency.Name}").ThenAsync(_ => dependency);
    }

    public async Task<Result> IsInstalled(Dependency dependency)
    {
        var result = await RunAptCommand($"dpkg -s {dependency.Name} | grep Status");

        if (!result.IsSuccess)
        {
            return (Result)result;
        }

        if (result.Value.Contains("install ok installed"))
        {
            return Result.Ok();
        }

        return Result.Fail($"Failed to install dependency: {dependency.Name}");
    }

    private static Task<Result<string>> RunAptCommand(string command)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        return Result.Capture(() => Process.Start(psi)).EnsureNotNull().ThenAsync(async process =>
        {
            await process.WaitForExitAsync();

            return Momad.ReturnAndDispose(process.StandardOutput.ReadToEnd(), process);
        });
    }
}
