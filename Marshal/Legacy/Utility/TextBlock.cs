using LinuxDedicatedServer.Api.Buffer.v1;
using System.Runtime.InteropServices;
using System.Text;

namespace LinuxDedicatedServer.Legacy.Utility;

public record TextBlock
{
    public bool HasColor { get; init; }
    public ConsoleColor Color { get; init; }
    public required string Text { get; init; }

    public static TextBlock Create(string text)
    {
        return new TextBlock { HasColor = false, Text = text };
    }

    public static TextBlock Create(string text, ConsoleColor color)
    {
        return new TextBlock { HasColor = true, Text = text, Color = color };
    }
}

public class TextBlocks
{
    public IEnumerable<TextBlock> Blocks { get; set; } = [];

    public override string ToString()
    {
        var builder = new StringBuilder();

        foreach (var block in Blocks)
        {
            builder.Append(block.Text);
        }

        return builder.ToString();
    }

    public void Print()
    {
        foreach (var block in Blocks)
        {
            if (block.HasColor)
            Console.ForegroundColor = block.Color;
            Console.Write(block.Text);
        }
    }
}

public class TextBlockBuilder
{
    List<TextBlock> _blocks = [];

    private TextBlockBuilder AppendTextBlock(string text, ConsoleColor? color = null)
    {
        _blocks.Add(color.HasValue ? TextBlock.Create(text, color.Value) : TextBlock.Create(text));
        return this;
    }

    public TextBlockBuilder AppendText(string text) => AppendTextBlock(text);
    public TextBlockBuilder AppendText(string text, ConsoleColor color) => AppendTextBlock(text, color);
    public TextBlockBuilder AppendLine(string text) => AppendTextBlock(text + "\n");
    public TextBlockBuilder AppendLine(string text, ConsoleColor color) => AppendTextBlock(text + "\n", color);
}

public class TextBlockResolver : IManagedTypeResolver
{
    public struct TextBlockBuffer
    {
        public bool HasColor;
        public byte Color;
        public byte[] Text;
    }

    public (object value, int length) ConvertValue(object value)
    {
        var block = value as TextBlock;
        byte[] text = Encoding.UTF8.GetBytes(block!.Text);
        int length = Marshal.SizeOf<bool>() + Marshal.SizeOf<byte>() + Marshal.SizeOf<byte>() * text.Length;

        return (new TextBlockBuffer { Text = text, HasColor = block.HasColor, Color = (byte)block.Color }, length);
    }

    public void ReadAddValue(BufferFactory factory, Type type, int length)
    {
        throw new NotImplementedException();
    }

    public object ReadValue(BufferReader reader, Type type, int length)
    {
        throw new NotImplementedException();
    }

    public void WriteAddValue(BufferFactory factory, Type type, object value)
    {
        throw new NotImplementedException();
    }

    public void WriteValue(BufferWriter writer, Type type, object value)
    {
        throw new NotImplementedException();
    }
}