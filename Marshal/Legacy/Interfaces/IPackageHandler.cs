using LinuxDedicatedServer.Legacy.Models;
using System.Net.Sockets;

namespace LinuxDedicatedServer.Legacy.Interfaces;

public interface IPackageHandler
{
    public Task<Package> PackageFolder(string folderPath, string packageName, double version);
    public Task<IEnumerable<Package>> GetPackages();
    public Task<Package> RecieveIncomingPackage(NetworkStream stream);
}
