namespace Graft.SmokeClient;

internal sealed class SmokeException : Exception
{
    public SmokeException(string code, string message, Exception? inner = null)
        : base(message, inner)
    {
        Code = code;
    }

    public string Code { get; }
}
