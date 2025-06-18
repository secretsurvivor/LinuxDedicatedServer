using LinuxDedicatedServer.Api.Buffer.v1;
using LinuxDedicatedServer.Legacy.Interfaces;
using LinuxDedicatedServer.Legacy.Models.Region;
using System.Net.Sockets;

namespace LinuxDedicatedServer.Legacy.Utility;

public class MessageFactory(IConfig config, ILoggerHandler logger, IPackageHandler package)
{
    public const uint CommandSignature = 0x434D444D;
    public const uint PackageSignature = 0x5041434B;

    private IConfig _config = config;
    private ILoggerHandler _logger = logger;

    public async Task<ICommandRegion> ReadMessage(TcpClient client)
    {
        using var stream = client.GetStream();
        var header = await ManagedBufferReader.ReadStruct<MessageHeader>(stream);

        var cmd = await command.RecieveIncomingCommand(stream);

        (NetworkMessageType messageType, INetworkPayload payload) = header.Signature switch
        {
            CommandSignature => (NetworkMessageType.Command, (INetworkPayload)await command.RecieveIncomingCommand(stream)),
            PackageSignature => (NetworkMessageType.Package, await package.RecieveIncomingPackage(stream)),
        };

        return NetworkMessage.Parse(header, messageType, payload);
    }

    // TODO List
    // Add optional argument values

    /// Command Examples:
    /// help
    /// 
    /// package upload --target "" [package]
    /// package delete --name ""
    /// package --list (Lists all current packages)
    /// 
    /// instance create --name "" --package ""

    public async Task<Command> RecieveIncomingCommand(NetworkStream stream)
    {
        var command = await ManagedBufferReader.ReadStruct<CommandStructure>(stream);

        List<CommandArgument> arguments = [];
        using var parser = new ArgumentParser(command.Arguments);

        while (parser.TryNextCommand(out var argument, out _))
        {
            arguments.Add(argument);
        }

        return new Command { Region = command.Region, Subcommand = command.Subcommand, Arguments = arguments };
    }
}

public struct CommandStructure
{
    public string Region;
    public string Subcommand;
    // This was a tough decision to make on how I was going to
    // communicate the arguments because obviously it should be
    // a dictionary but I think anything more than a string would
    // just over complicate such a simple need.
    public string Arguments;
}

public struct MessageHeader
{
    public uint Signature;
    public double Version;
}
