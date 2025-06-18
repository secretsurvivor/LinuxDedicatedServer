using LinuxDedicatedServer.Legacy.Models;
using LinuxDedicatedServer.Legacy.Models.Region;
using MediatR;

namespace LinuxDedicatedServer.Legacy.Handlers;

public class HelpHandler : IRequestHandler<HelpRegion, CommandResult>
{
    public Task<CommandResult> Handle(HelpRegion request, CancellationToken cancellationToken)
    {
        
    }


}
