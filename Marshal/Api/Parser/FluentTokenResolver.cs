namespace LinuxDedicatedServer.Api.Parser;

public class FluentTokenResolver
{
    private readonly List<TokenRule> _rules = [];
    private Func<Token>? _eofFactory;
    private Func<Token>? _default;

    public static FluentTokenResolver Create()
    {
        return new FluentTokenResolver();
    }

    public FluentTokenResolver IfCharacter(Func<int, bool> predicate)
    {
        _rules.Add(new TokenRule { Predicate = predicate });
        return this;
    }

    public FluentTokenResolver OnCharacter(int character)
    {
        _rules.Add(new TokenRule { Predicate = x => x == character });
        return this;
    }

    public FluentTokenResolver WithToken(Func<Token> func)
    {
        if (_rules.Count <= 0)
        {
            throw new InvalidOperationException("No character condition has been set before assigning a token.");
        }

        ArgumentNullException.ThrowIfNull(func);

        _rules[^1].TokenFactory = func;
        return this;
    }

    public FluentTokenResolver WithDefaultToken(Func<Token> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        _default = func;
        return this;
    }

    public FluentTokenResolver WithEndOfFileToken(Func<Token> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        _eofFactory = func;
        return this;
    }

    public Token Resolve(int character)
    {
        if (character == -1)
        {
            return _eofFactory?.Invoke() ?? throw new InvalidOperationException("Missing End of File token factory");
        }

        foreach (var rule in _rules)
        {
            if (rule.Predicate(character))
            {
                return rule.TokenFactory?.Invoke() ?? throw new InvalidOperationException("Missing token factory for predicate");
            }
        }

        return _default?.Invoke() ?? throw new InvalidOperationException("Missing default token factory");
    }

    private class TokenRule
    {
        public required Func<int, bool> Predicate { get; init; }
        public Func<Token>? TokenFactory { get; set; }
    }
}