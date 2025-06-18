using System.Text;

namespace LinuxDedicatedServer.Api.Parser;

public abstract class AbstractTokenParser : IDisposable
{
    public AbstractTokenParser(Stream stream, TokenParserOptions options)
    {
        _reader = stream;
        Options = options;
    }

    // character tabulation, line feed, line tabulation, form feed, carriage return, space, next line, no-break space
    private static readonly IEnumerable<int> WhitespaceCharacters = [9, 10, 11, 12, 13, 32, 133, 160];

    protected int CurrentChar { get; private set; }
    private int? _peakedChar;
    protected Token CurrentToken { get; private set; }
    private Token? _peakedToken;
    protected Token LastToken { get; private set; }

    private readonly Stream _reader;

    protected bool IsEndOfFile { get => CurrentChar == -1; }
    private TokenParserOptions Options { get; }

    protected static bool IsUpperLetter(int @char) => @char >= 65 && @char <= 90;
    protected static bool IsLowerLetter(int @char) => @char >= 97 && @char <= 122;
    protected static bool IsLetter(int @char) => IsUpperLetter(@char) || IsLowerLetter(@char);
    protected static bool IsNumber(int @char) => @char >= 48 && @char <= 57;
    protected static bool IsNumberOrLetter(int @char) => IsLetter(@char) || IsNumber(@char);
    protected static bool IsWhitespace(int @char) => WhitespaceCharacters.Contains(@char);

    protected int PeakChar()
    {
        if (!_peakedChar.HasValue)
        {
            _peakedChar = _reader.ReadByte();
        }

        return _peakedChar.Value;
    }

    protected void NextChar()
    {
        if (_peakedChar.HasValue)
        {
            CurrentChar = _peakedChar.Value;
            _peakedChar = null;
        }
        else
        {
            CurrentChar = _reader.ReadByte();
        } 

        if (Options.AsciiOnly && CurrentChar > 255)
        {
            throw new NotSupportedException("This parser does not support more than ascii characters");
        }
    }

    protected string ReadUntil(int @char, bool includeFirst = true, bool skipLast = false)
    {
        var builder = new StringBuilder();

        if (!includeFirst)
        {
            NextChar();
        }

        while (!IsEndOfFile)
        {
            builder.Append((char)CurrentChar);

            if (PeakChar() == @char)
            {
                break;
            }

            NextChar();
        }

        if (skipLast)
        {
            NextChar();
        }

        return builder.ToString();
    }

    protected string ReadWhile(Func<int, bool> predicate, bool includeFirst = true, bool skipLast = false)
    {
        var builder = new StringBuilder();

        if (!includeFirst)
        {
            NextChar();
        }

        while (!IsEndOfFile)
        {
            builder.Append((char)CurrentChar);

            if (!predicate(PeakChar()))
            {
                break;
            }

            NextChar();
        }

        if (skipLast)
        {
            NextChar();
        }

        return builder.ToString();
    }

    protected string ReadUntilWhitespace(bool includeFirst = false, bool skipLast = false)
    {
        var builder = new StringBuilder();

        if (!includeFirst)
        {
            NextChar();
        }

        while (!IsEndOfFile)
        {
            builder.Append((char)CurrentChar);

            if (WhitespaceCharacters.Contains(CurrentChar))
            {
                break;
            }

            NextChar();
        }

        if (skipLast)
        {
            NextChar();
        }

        return builder.ToString();
    }

    protected abstract Token ResolveToken();

    private Token GetNextToken()
    {
        do
        {
            NextChar();
        } while (Options.IgnoreWhitespace && WhitespaceCharacters.Contains(CurrentChar) && !IsEndOfFile);

        return ResolveToken();
    }

    protected Token PeakToken()
    {
        if (!_peakedToken.HasValue)
        {
            _peakedToken = GetNextToken();
        }

        return _peakedToken.Value;
    }

    protected void NextToken()
    {
        if (_peakedToken.HasValue)
        {
            LastToken = CurrentToken;
            CurrentToken = _peakedToken.Value;
            _peakedToken = null;

            return;
        }

        LastToken = CurrentToken;
        CurrentToken = GetNextToken();
    }

    protected bool TryNextToken(int expectedId)
    {
        NextToken();
        return CurrentToken.Id == expectedId;
    }

    protected bool TryNextToken(Func<int, bool> predicate)
    {
        NextToken();
        return predicate(CurrentToken.Id);
    }

    protected bool TryPeakToken(int expectedId)
    {
        return PeakToken().Id == expectedId;
    }

    protected bool TryPeakToken(Func<int, bool> predicate)
    {
        return predicate(PeakToken().Id);
    }

    protected bool TryPeakAndConsume(int expectedId)
    {
        bool result = PeakToken().Id == expectedId;

        if (result)
        {
            NextToken();
        }

        return result;
    }

    protected bool TryPeakAndConsume(Func<int, bool> predicate)
    {
        bool result = predicate(PeakToken().Id);

        if (result)
        {
            NextToken();
        }

        return result;
    }

    public void Dispose()
    {
        _reader.Dispose();
        GC.SuppressFinalize(this);
    }

    protected TokenParserPipeline Parse()
    {
        return new TokenParserPipeline(this);
    }

    protected class TokenParserPipeline(AbstractTokenParser parser)
    {
        private readonly AbstractTokenParser _parser = parser;
        private bool _isValid = true;
        private string _failureMessage = string.Empty;

        public TokenParserPipeline RequireToken(int id)
        {
            if (_isValid) _isValid = _parser.TryPeakAndConsume(id);
            return this;
        }

        public TokenParserPipeline RequireToken(int id, string failureMessage)
        {
            if (_isValid)
            {
                _isValid = _parser.TryPeakAndConsume(id);

                if (!_isValid)
                {
                    _failureMessage = failureMessage;
                }
            }

            return this;
        }

        public TokenParserPipeline RequireToken(Func<int, bool> predicate)
        {
            if (_isValid) _isValid = _parser.TryPeakAndConsume(predicate);
            return this;
        }

        public TokenParserPipeline RequireToken(Func<int, bool> predicate, string failureMessage)
        {
            if (_isValid)
            {
                _isValid = _parser.TryPeakAndConsume(predicate);

                if (!_isValid)
                {
                    _failureMessage = failureMessage;
                }
            }

            return this;
        }

        public TokenParserPipeline IfToken(int id, out bool value)
        {
            value = _isValid && _parser.TryPeakAndConsume(id);
            return this;
        }

        public TokenParserPipeline Let<T>(out T value, Func<Token, T> selector)
        {
            value = _isValid ? selector(_parser.CurrentToken) : default!;
            return this;
        }

        public bool Return()
        {
            return _isValid;
        }

        public bool Return<T>(Func<T> resultFactory, out T result)
        {
            result = _isValid ? resultFactory() : default!;
            return _isValid;
        }

        public bool Return<T>(Func<T> resultFactory, out T result, out string failureMessage)
        {
            result = _isValid ? resultFactory() : default!;
            failureMessage = _isValid ? _failureMessage : default!;
            return _isValid;
        }
    }
}

public record TokenParserOptions(bool IgnoreWhitespace = true, bool AsciiOnly = true);
