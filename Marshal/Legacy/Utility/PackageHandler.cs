using LinuxDedicatedServer.Api.Buffer.v1;
using LinuxDedicatedServer.Legacy.Interfaces;
using LinuxDedicatedServer.Legacy.Models;
using System.IO.Compression;
using System.Net.Sockets;

namespace LinuxDedicatedServer.Legacy.Utility;

public class PackageHandler(IConfig config, ILoggerHandler logger) : IPackageHandler
{
    public static uint PackageSignature { get; } = 0x5F504B1C;
    private readonly IConfig _config = config;
    private readonly ILoggerHandler _logger = logger;

    public static string BuildPackageFilename(string name, double version)
    {
        return $"{name}-{version}";
    }

    public static async Task<PackageHeader> GetPackageHeader(Stream stream)
    {
        var header = await ManagedBufferReader.ReadStruct<PackageHeader>(stream);

        if (!header.IsSuccess)
        {
            throw new ArgumentException("Invalid package data stream", nameof(stream));
        }

        if (header.Value.Signature != PackageSignature)
        {
            throw new ArgumentException("Invalid package data stream", nameof(stream));
        }

        return header.Value;
    }

    public async Task<Package> PackageFolder(string folderPath, string packageName, double version)
    {
        if (!Directory.Exists(folderPath))
        {
            throw new ArgumentException("Invalid folderpath");
        }

        var filename = BuildPackageFilename(packageName, version);
        var filePath = Path.Combine(_config.PackageDirectory, filename);

        if (File.Exists(filePath))
        {
            throw new ArgumentException($"Package name with specific version {version} already exists");
        }

        var header = new PackageHeader() { Name = packageName, DateCreated = DateTime.UtcNow, Signature = PackageSignature, Version = version };
        using var writeStream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write);
        
        await ManagedBufferWriter.WriteStruct(writeStream, header);

        await writeStream.FlushAsync();

        using var archive = new ZipArchive(writeStream, ZipArchiveMode.Create);

        foreach (var file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
        {
            string entryName = file.Substring(folderPath.Length + 1).Replace("\\", "/");
            archive.CreateEntryFromFile(file.ToString(), entryName.ToString());
        }

        await writeStream.FlushAsync();

        return Package.Parse(header, filePath);
    }

    public async Task<IEnumerable<Package>> GetPackages()
    {
        var packages = new List<Package>();

        foreach (var file in Directory.EnumerateFiles(_config.PackageDirectory))
        {
            using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
            var result = await ManagedBufferReader.ReadStruct<PackageHeader>(fileStream);

            if (!result.IsSuccess)
            {
                await _logger.Warning($"Invalid file in package directory: {file}");
                continue;
            }
            
            packages.Add(Package.Parse(result.Value, file));
        }

        return packages;
    }

    public async Task<Package> RecieveIncomingPackage(NetworkStream stream)
    {
        // Validate
        var header = await GetPackageHeader(stream);

        // Save
        string fileLocation = Path.Combine(_config.PackageDirectory, BuildPackageFilename(header.Name, header.Version));
        using var writeStream = new FileStream(fileLocation, FileMode.Create, FileAccess.Write);

        await ManagedBufferWriter.WriteStruct(writeStream, header);
        await stream.FlushAsync();

        await stream.CopyToAsync(writeStream);

        return Package.Parse(header, fileLocation);
    }
}