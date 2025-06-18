namespace DeltaForge;

public class DeltaAnalyser(string existingPath, string replacementPath)
{


    public async IAsyncEnumerable<IDeltaInstruction> GenerateInstructions()
    {
        using var existingStream = new FileStream(existingPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 512, useAsync: true);
        using var replacementStream = new FileStream(replacementPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 512, useAsync: true);


    }
}

public interface IDeltaInstruction
{
    public int Position { get; set; }
    public int Size { get; set; }
    public ulong Hash { get; set; }

    public Span<byte> GetBytes();
}

public struct DeltaInstruction : IDeltaInstruction
{
    public int Position { get; set; }
    public int Size { get; set; }

}

public class DeltaSnapshot : IDeltaInstruction
{

}
