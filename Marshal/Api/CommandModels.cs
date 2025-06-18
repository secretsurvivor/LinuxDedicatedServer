using LinuxDedicatedServer.Api.Parser;
using System.Text;

namespace LinuxDedicatedServer.Api;

[Serializable]
public class Command
{
    public required string GroupName { get; init; }
    public string? ActionName { get; init; }
    public required IEnumerable<CommandArgument> Arguments { get; init; }
}

public class CommandArgument
{
    public required bool Condensed { get; init; }
    public required string Key { get; init; }
    public string? Value { get; init; }
}

public class CommandParser(Stream command) : AbstractTokenParser(command, _options), IAsyncParser<Command>
{
    private static readonly TokenParserOptions _options = new TokenParserOptions
    {
        IgnoreWhitespace = true,
        AsciiOnly = true,
    };

    public static bool ParseCommand(string text, out Command command)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        using var parser = new CommandParser(stream);

        return parser.TryNextCommand(out command);
    }

    public static bool ParseCommand(Stream stream, out Command command)
    {
        using var parser = new CommandParser(stream);

        return parser.TryNextCommand(out command);
    }

    public static async ParseResult<Command> ParseCommand(Stream stream)
    {
        using var parser = new CommandParser(stream);

        return await parser.ParseAsync();
    }

    public static bool ParseHeader(Stream stream, out string group, out string? action)
    {
        using var parser = new CommandParser(stream);

        return parser.TryNextHeader(out group, out action);
    }

    public static IEnumerable<CommandArgument> ParseArguments(Stream stream)
    {
        using var parser = new CommandParser(stream);

        return parser.GetArguments();
    }

    public ParseResult<Command> ParseAsync()
    {
        if (TryNextCommand(out var command))
        {
            return ParseResult<Command>.Success(command);
        }

        return ParseResult<Command>.Failure("Failed to parse command");
    }

    // Grammar
    // command = text ?text *argument
    // argument = dash ?dash text ?( text | escaped )

    public bool TryNextHeader(out string group, out string? action)
    {
        group = default!;
        action = default;

        if (!TryPeakAndConsume(0))
            return false;

        group = CurrentToken.RawValue;

        if (!TryPeakAndConsume(0))
            return true;

        action = CurrentToken.RawValue;

        return true;
    }

    public bool TryNextCommand(out Command command)
    {
        command = default!;

        if (!TryNextHeader(out string group, out string? action))
            return false;

        var arguments = GetArguments();

        command = new Command { GroupName = group, ActionName = action, Arguments = arguments };

        return true;
    }

    public IEnumerable<CommandArgument> GetArguments()
    {
        IList<CommandArgument> arguments = [];

        while (TryNextArgument(out var argument))
        {
            arguments.Add(argument);
        }

        return arguments;
    }

    public bool TryNextArgument(out CommandArgument argument)
    {
        argument = default!;

        if (!TryPeakAndConsume(1))
            return false;

        bool hasDoubleDash = TryPeakAndConsume(1);

        if (!TryPeakAndConsume(0))
            return false;

        string argumentName = CurrentToken.RawValue;

        if (!TryPeakAndConsume(i => i == 0 || i == 2))
        {
            argument = new CommandArgument { Condensed = !hasDoubleDash, Key = argumentName };
            return true;
        }

        string argumentValue = CurrentToken.RawValue;

        argument = new CommandArgument { Condensed = !hasDoubleDash, Key = argumentName, Value = argumentValue };
        return true;
    }

    // Token Ids
    // 0: text
    // 1: dash
    // 2: escaped
    // -2 : error
    // -1 : eof

    private const int DASH = '-';
    private const int DQUOTE = '"';

    private static FluentTokenResolver? _resolver;

    private FluentTokenResolver GetResolver()
    {
        return _resolver ??= FluentTokenResolver.Create()
            .IfCharacter(IsLetter)
            .WithToken(() =>
            {
                var value = ReadWhile(IsNumberOrLetter);
                return Token.Create(0, value);
            })
            .OnCharacter(DASH)
            .WithToken(() => Token.Create(1))
            .OnCharacter(DQUOTE)
            .WithToken(() =>
            {
                var value = ReadUntil(DQUOTE);
                return Token.Create(2, value);
            })
            .WithDefaultToken(() => Token.Create(-2))
            .WithEndOfFileToken(() => Token.Create(-1));
    }

    protected override Token ResolveToken()
    {
        return GetResolver().Resolve(CurrentChar);
    }
}
