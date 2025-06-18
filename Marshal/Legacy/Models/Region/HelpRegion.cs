namespace LinuxDedicatedServer.Legacy.Models.Region;

public class HelpRegion : ICommandRegion
{
    public required string Subcommand { get; init; }

    public bool ValidateArguments(out string errorMessage)
    {
        throw new NotImplementedException();
    }

    public bool ValidateSubcommand(out string errorMessage)
    {
        throw new NotImplementedException();
    }
}
