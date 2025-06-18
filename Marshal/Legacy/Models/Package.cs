using LinuxDedicatedServer.Legacy.Interfaces;

namespace LinuxDedicatedServer.Legacy.Models;

public class Package : INetworkPayload
{
    public required string Name { get; init; }
    public required double Version { get; init; }
    public required DateTime DateCreated { get; init; }
    public required string Location { get; init; }

    public FileStream Open()
    {
        return new FileStream(Location, FileMode.Open, FileAccess.Read);
    }

    public static Package Parse(PackageHeader header, string filePath)
    {
        return new Package
        {
            Name = header.Name,
            Version = header.Version,
            DateCreated = header.DateCreated,
            Location = filePath,
        };
    }
}

public struct PackageHeader
{
    public uint Signature;
    public DateTime DateCreated;
    public double Version;
    public string Name;
}