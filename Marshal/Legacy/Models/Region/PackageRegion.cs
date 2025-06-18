using LinuxDedicatedServer.Legacy.Models;
using MediatR;

namespace LinuxDedicatedServer.Legacy.Models.Region;

public class PackageRegion : ICommandRegion
{
    // Region = "Package"
    public required string Subcommand { get; init; }
    public required IEnumerable<CommandArgument> Arguments { get; init; }

    public bool ValidateArguments(out string errorMessage)
    {
        
    }

    public bool ValidateSubcommand(out string errorMessage)
    {
        throw new NotImplementedException();
    }
}

public interface ICommandRegion : IRequest<CommandResult>
{
    public string Subcommand { get; init;}

    public bool ValidateSubcommand(out string errorMessage);
    public bool ValidateArguments(out string errorMessage);
}