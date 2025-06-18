using LinuxDedicatedServer.Api;
using Marshal.Api;

namespace LinuxDedicatedServer.Controllers;

[CommandController("INSTANCE")]
public class InstanceController(InstanceManager instanceManager, PackageManager packageManager) : CommandController
{
    public override Task<ICommandResult> Default(IEnumerable<CommandArgument> args)
    {
        throw new NotImplementedException();
    }

    [Command("LIST")] // "List all active instances"
    public async Task<ICommandResult> ListInstances()
    {
        throw new NotImplementedException();
    }

    [Command("START")] // "Start a new instance with the given metadata"
    public async Task<ICommandResult> StartInstance(IEnumerable<CommandArgument> args)
    {
        throw new NotImplementedException();
    }
}
