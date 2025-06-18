namespace LinuxDedicatedServer.Api.Parser;

public class TokenReaderFactory
{
    public TokenReaderFactory() { }

    private TokenReaderFactory(FluentTokenResolver resolver)
    {
        _fluentTokenResolver = resolver;
    }

    public static TokenReaderFactory CreateWithResolver(Func<FluentTokenResolver, FluentTokenResolver> func)
    {
        return new TokenReaderFactory(func.Invoke(FluentTokenResolver.Create()));
    }

    private FluentTokenResolver? _fluentTokenResolver;
    private TokenParserOptions _options = new();

    public TokenReaderFactory WithResolver(Func<FluentTokenResolver, FluentTokenResolver> func)
    {
        _fluentTokenResolver = func.Invoke(FluentTokenResolver.Create());
        return this;
    }

    public TokenReaderFactory WithOptions(Func<TokenParserOptions> func)
    {
        _options = func.Invoke();
        return this;
    }

    public TokenReader Parse(Stream input)
    {
        if (_fluentTokenResolver is null)
        {
            throw new InvalidOperationException("Resolver must be established before anything can be parsed.");
        }

        return new TokenReader(_fluentTokenResolver, input, _options);
    }

    public class TokenReader(FluentTokenResolver resolver, Stream data, TokenParserOptions options) : AbstractTokenParser(data, options)
    {
        protected override Token ResolveToken()
        {
            return resolver.Resolve(CurrentChar);
        }

        public Token ReadNextToken()
        {
            NextToken();
            return CurrentToken;
        }

        public new Token PeakToken() => base.PeakToken();

        public new bool TryNextToken(int id) => base.TryNextToken(id);

        public new bool TryNextToken(Func<int, bool> predicate) => base.TryNextToken(predicate);

        public new bool TryPeakToken(int id) => base.TryPeakToken(id);
    }
}
