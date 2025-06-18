using LinuxDedicatedServer.Api;
using LinuxDedicatedServer.Api.Buffer.v1;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text;

namespace Marshal.Api;

public interface IPackageManager
{
    public IAsyncEnumerable<Package> GetAllPackages();
    public IAsyncEnumerable<Package> GetPackages(string name);
    public Task<Result<Package>> ImportPackage(Stream stream);
    public Task<Result<Package>> GetPackage(string name, string version);
    public Task UnpackPackage(Package package, string directory);
}

public class PackageManagerConfig
{
    public required string PackagePath { get; init; }
}

public class PackageManager(PackageManagerConfig config, IProfiler<PackageManager> profiler, ILogger<PackageManager> logger) : IPackageManager
{
    public async IAsyncEnumerable<Package> GetAllPackages()
    {
        var files = Directory.GetFiles(config.PackagePath, $"*.pack", new EnumerationOptions { RecurseSubdirectories = false });

        foreach (var file in files)
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
            var result = await Result.CaptureAsync(() => ManagedBufferReader.ReadStruct<PackageHeader>(stream));

            // Ignore files with invalid headers
            if (!result.IsSuccess)
            {
                continue;
            }

            yield return Package.Create(result.Value, file);
        }
    }

    public async IAsyncEnumerable<Package> GetPackages(string name)
    {
        await foreach (var package in GetAllPackages())
        {
            if (package.Name == name)
            {
                yield return package;
            }
        }
    }

    public async Task<Result<Package>> ImportPackage(Stream stream)
    {
        using var scope = profiler.Profile("ImportPackage(Stream)");
        var result = await Result.CaptureAsync(() => ManagedBufferReader.ReadStruct<PackageHeader>(stream));

        // Invalid header
        if (!result.IsSuccess)
        {
            return Result.Fail<Package>("Invalid package header");
        }
        
        var path = Path.Combine(config.PackagePath, $"{result.Value.Name}.{result.Value.Version}.pack");

        // Package already exists
        if (File.Exists(path)) 
        {
            return Result.Fail<Package>($"Package '{result.Value.Name}.{result.Value.Version}' already exists");
        }

        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        logger.Log(LogLevel.Information, "Imported package '{name}.{verion}.pack'", result.Value.Name, result.Value.Version);

        await stream.CopyToAsync(file);

        return Result.Ok(Package.Create(result.Value, path));
    }

    public async Task<Result<Package>> GetPackage(string name, string version)
    {
        await foreach (var package in GetAllPackages())
        {
            if (package.Name.Equals(name) && package.Version.Equals(version))
            {
                return Result.Ok(package);
            }
        }

        return Result.Fail<Package>($"Invalid package '{name}.{version}'");
    }

    public async Task UnpackPackage(Package package, string directory)
    {
        using var scope = profiler.Profile("UnpackPackage");

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var packageContent = package.OpenContents();
        using var archive = new ZipArchive(packageContent, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            var path = Path.Combine(directory, entry.FullName);
            var directoryName = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            if (string.IsNullOrEmpty(entry.Name))
                continue;

            using var entryStream = entry.Open();
            using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true);

            await entryStream.CopyToAsync(fileStream);
        }
    }

    private readonly struct HeaderResult
    {
        public readonly bool Success { get; init; }
        public readonly PackageHeader Header { get; init; }
    }
}

public struct PackageHeader
{
    public string Name;
    public string Version;
    public DateTime Created;

    public readonly int CalculateSize()
    {
        int nameByteCount = Encoding.UTF8.GetByteCount(Name);
        int versionByteCount = Encoding.UTF8.GetByteCount(Version);

        return Marshal.SizeOf<int>() + nameByteCount + Marshal.SizeOf<int>() + versionByteCount + Marshal.SizeOf<long>() + Marshal.SizeOf<int>();
    }

    public readonly override string ToString()
    {
        return $"Name: {Name}, Version: {Version}, Created: {Created}";
    }
}

public class Package
{
    public PackageHeader Header { private get; init; }
    public string Name { get => Header.Name; }
    public string Version { get => Header.Version; }
    public DateTime Created { get => Header.Created; }
    public required string Location { get; init; }

    public FileStream Open()
    {
        return new FileStream(Location, FileMode.Open, FileAccess.Read);
    }

    public FileStream OpenContents()
    {
        var file = Open();

        var size = Header.CalculateSize();
        file.Seek(size, SeekOrigin.Begin);

        return file;
    }

    public static Package Create(PackageHeader header, string path)
    {
        return new Package { Header = header, Location = path };
    }
}