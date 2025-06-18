namespace LinuxDedicatedServer.Api.Parser;

public readonly struct Token
{
    public int Id { get; init; }
    public string RawValue { get; init; }
    public object Value { get; init; }

    public T ValueAs<T>()
    {
        return (T)Value;
    }

    public static Token Create(int id)
    {
        return new Token { Id = id };
    }

    public static Token Create(int id, string value)
    {
        return new Token { Id = id, RawValue = value, Value = value };
    }

    public static Token Create(int id, string value, object actualValue)
    {
        return new Token { Id = id, RawValue = value, Value = actualValue };
    }

    public override string ToString() => $"Id = {Id}, RawValue = {RawValue ?? "null"}, Value = {Value ?? "null"}";
}