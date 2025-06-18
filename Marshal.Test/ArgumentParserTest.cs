using LinuxDedicatedServer.Models;
using LinuxDedicatedServer.Utility.Parser;

namespace LinuxDedicatedServer.Test
{
    public class ArgumentParserTest
    {
        private static bool TryCommand(string data, out CommandArgument argument)
        {
            using var parser = new ArgumentParser(data);
            return parser.TryNextCommand(out argument);
        }

        [Fact]
        public void ArgumentParser_CanParseClean()
        {
            var data = "-test value";

            var result = TryCommand(data, out var argument);

            Assert.True(result);
            Assert.False(argument.Condensed);
            Assert.Equal("test", argument.Key);
            Assert.Equal("value", argument.Value);
        }

        [Fact]
        public void ArgumentParser_CanParseQuotes()
        {
            var data = "-test \"Hello World\"";

            var result = TryCommand(data, out var argument);

            Assert.True(result);
            Assert.False(argument.Condensed);
            Assert.Equal("test", argument.Key);
            Assert.Equal("Hello World", argument.Value);
        }

        [Fact]
        public void ArgumentParser_CanParseDouble()
        {
            var data = "--test hi";

            var result = TryCommand(data, out var argument);

            Assert.True(result);
            Assert.True(argument.Condensed);
            Assert.Equal("test", argument.Key);
            Assert.Equal("hi", argument.Value);
        }

        [Fact]
        public void ArgumentParser_CanParseDirty()
        {
            var data = "   -   test      \"another    test\" ";

            var result = TryCommand(data, out var argument);

            Assert.True(result);
            Assert.False(argument.Condensed);
            Assert.Equal("test", argument.Key);
            Assert.Equal("another    test", argument.Value);
        }

        [Fact]
        public void ArgumentParser_MultipleArguments()
        {
            var data = "-one \"value of first argument\" -two \"value of second argument\" --three \"value of third argument\" ";

            using var parser = new ArgumentParser(data);

            var firstResult = parser.TryNextCommand(out var firstArgument);
            var secondResult = parser.TryNextCommand(out var secondArgument);
            var thirdResult = parser.TryNextCommand(out var thirdArgument);

            Assert.True(firstResult);
            Assert.Equal("one", firstArgument.Key);
            Assert.Equal("value of first argument", firstArgument.Value);

            Assert.True(secondResult);
            Assert.Equal("two", secondArgument.Key);
            Assert.Equal("value of second argument", secondArgument.Value);

            Assert.True(thirdResult);
            Assert.Equal("three", thirdArgument.Key);
            Assert.Equal("value of third argument", thirdArgument.Value);
        }
    }
}
