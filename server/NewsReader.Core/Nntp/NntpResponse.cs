namespace NewsReader.Core.Nntp;

public sealed record NntpResponse(int Code, string Message)
{
    public bool IsPositiveCompletion => Code is >= 200 and < 300;
    public bool IsPositiveIntermediate => Code is >= 300 and < 400;
    public bool IsNegative => Code >= 400;

    public static NntpResponse Parse(string line)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(line);

        if (line.Length < 3 || !int.TryParse(line.AsSpan(0, 3), out var code))
        {
            throw new FormatException($"Invalid NNTP response line: '{line}'");
        }

        var message = line.Length > 4 ? line[4..] : string.Empty;
        return new NntpResponse(code, message);
    }
}
