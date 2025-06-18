using LinuxDedicatedServer.Utility.Parser;

namespace LinuxDedicatedServer.Test;

public class AbstractTokenParserTest
{
    [Fact]
    public void ExampleTokenParser_Plus()
    {
        var data = "1 + 2";

        var result = ExampleTokenParser.CalculateEquation(data);

        Assert.Equal(3, result);
    }

    [Fact]
    public void ExampleTokenParser_Minus()
    {
        var data = "8 - 3";

        var result = ExampleTokenParser.CalculateEquation(data);

        Assert.Equal(5, result);
    }

    [Fact]
    public void ExampleTokenParser_OddWhitespace()
    {
        var data = "  122+1";

        var result = ExampleTokenParser.CalculateEquation(data);

        Assert.Equal(123, result);
    }

    [Fact]
    public void ExampleTokenParser_InvalidCharacterThrowsException()
    {
        var data = "1+a";

        Assert.Throws<InvalidDataException>(() => ExampleTokenParser.CalculateEquation(data));
    }

    [Fact]
    public void ExampleTokenParserPipeline_Plus()
    {
        var data = "1 + 2";

        var result = ExampleTokenParser.CalculateEquationPipeline(data);

        Assert.Equal(3, result);
    }

    [Fact]
    public void ExampleTokenParserPipeline_Minus()
    {
        var data = "8 - 3";

        var result = ExampleTokenParser.CalculateEquationPipeline(data);

        Assert.Equal(5, result);
    }

    [Fact]
    public void ExampleTokenParserPipeline_OddWhitespace()
    {
        var data = "  122+1";

        var result = ExampleTokenParser.CalculateEquationPipeline(data);

        Assert.Equal(123, result);
    }

    [Fact]
    public void ExampleTokenParserPipeline_InvalidCharacterThrowsException()
    {
        var data = "1+a";

        Assert.Throws<InvalidDataException>(() => ExampleTokenParser.CalculateEquationPipeline(data));
    }
}

public sealed class ExampleTokenParser(string data) : AbstractTokenParser(data, Options)
{
    private static readonly TokenParserOptions Options = new TokenParserOptions
    {
        IgnoreWhitespace = true,
        AsciiOnly = true,
    };

    public static int CalculateEquation(string equation)
    {
        using var parser = new ExampleTokenParser(equation);

        if (!parser.CalculateString(out int output, out var errorMessage))
        {
            throw new InvalidDataException(errorMessage);
        }

        return output;
    }

    public static int CalculateEquationPipeline(string equation)
    {
        using var parser = new ExampleTokenParser(equation);

        if (!parser.CalculateStringWithPipeline(out int output, out string errorMessage))
        {
            throw new InvalidDataException("Failed to calculate equation");
        }

        return output;
    }

    public bool CalculateString(out int output, out string errorMessage)
    {
        if (!TryNextToken(0))
        {
            output = default;
            errorMessage = "Missing first number";
            return false;
        }

        int value1 = CurrentToken.ValueAs<int>();

        if (!TryNextToken(x => x >= 1 && x <= 4))
        {
            output = default;
            errorMessage = "Missing modifier";
            return false;
        }

        int modifier = CurrentToken.Id;

        if (!TryNextToken(0))
        {
            output = default;
            errorMessage = "Missing second number";
            return false;
        }

        int value2 = CurrentToken.ValueAs<int>();

        output = modifier switch
        {
            1 => value1 + value2,
            2 => value1 - value2,
            3 => value1 * value2,
            4 => value1 / value2,
        };

        errorMessage = default;
        return true;
    }

    public bool CalculateStringWithPipeline(out int output, out string errorMessage)
    {
        return Parse()
            .RequireToken(0, "Missing first number")
            .Let(out int value1, x => x.ValueAs<int>())
            .RequireToken(x => x >= 1 && x <= 4, "Missing modifier")
            .Let(out int modifier, x => x.Id)
            .RequireToken(0, "Missing second number")
            .Let(out int value2, x => x.ValueAs<int>())
            .Return(() => modifier switch
            {
                1 => value1 + value2,
                2 => value1 - value2,
                3 => value1 * value2,
                4 => value1 / value2,
            }, out output, out errorMessage);
    }

    /// Token Recognising
    /// EQ: NUM ( PLUS | MINUS | MULT | DIV ) NUM
    /// 
    /// NUM: INT ( DOT INT )
    /// 
    /// -2: EoF
    /// -1: Error
    /// 0: NUM 
    /// 1: PLUS
    /// 2: MINUS
    /// 3: MULT
    /// 4: DIV

    private const int PLUS = '+';
    private const int MINUS = '-';
    private const int MULT = '*';
    private const int DIV = '/';

    private FluentTokenResolver? _resolver;

    private FluentTokenResolver GetResolver()
    {
        return _resolver ??= FluentTokenResolver.Create()
            .IfCharacter(IsNumber)
            .WithToken(() =>
            {
                var value = ReadWhile(IsNumber, true);
                return Token.Create(0, value, int.Parse(value));
            })

            .OnCharacter(PLUS)
            .WithToken(() => Token.Create(1))

            .OnCharacter(MINUS)
            .WithToken(() => Token.Create(2))

            .OnCharacter(MULT)
            .WithToken(() => Token.Create(3))

            .OnCharacter(DIV)
            .WithToken(() => Token.Create(4))

            .WithEndOfFileToken(() => Token.Create(-2))
            .WithDefaultToken(() => Token.Create(-1, "Unrecognised character"));
    }

    protected override Token ResolveToken()
    {
        return GetResolver().Resolve(CurrentChar);
    }
}