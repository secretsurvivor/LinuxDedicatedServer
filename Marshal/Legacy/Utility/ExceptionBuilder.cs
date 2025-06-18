using LinuxDedicatedServer.Legacy.Models;

namespace LinuxDedicatedServer.Legacy.Utility
{
    public interface IExceptionBuilder
    {
        IPackageExistsExceptionData ForPackageExists();
    }

    public class ExceptionBuilder : IExceptionBuilder, IPackageExistsExceptionData
    {
        public string PackageName { get; set; }

        public IPackageExistsExceptionData ForPackageExists()
        {
            return this;
        }
    }

    public abstract class HostedException : Exception
    {
        public HostedException() { }
        public HostedException(string? message) : base(message) { }
        public HostedException(string? message, Exception? innerException) : base(message, innerException) { }

        public virtual LogRecord GetLogRecord()
        {
            return new LogRecord(DateTime.UtcNow, "Error", Message);
        }
    }

    public interface IPackageExistsExceptionData
    {
        public string PackageName { get; set; }

        public IPackageExistsExceptionData WithPackageName(string name)
        {
            PackageName = name;
            return this;
        }
    }

    public class PackageExistsException : HostedException
    {
        private static string MakeMessage(IPackageExistsExceptionData data)
        {
            return $"Package '{data.PackageName}' already exists.";
        }

        public PackageExistsException(IPackageExistsExceptionData data) : base(MakeMessage(data)) { }
        public PackageExistsException(IPackageExistsExceptionData data, Exception innerException) : base(MakeMessage(data), innerException) { }
    }

    public class InvalidPackage : HostedException
    {

    }
}
