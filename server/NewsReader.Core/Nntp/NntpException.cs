namespace NewsReader.Core.Nntp;

public sealed class NntpException : Exception
{
    public NntpException(string message)
        : base(message) { }

    public NntpException(int code, string message)
        : base($"NNTP error {code}: {message}")
    {
        Code = code;
        ServerMessage = message;
    }

    public int? Code { get; }
    public string? ServerMessage { get; }
}
