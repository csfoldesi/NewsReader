using System.Text;
using System.Text.RegularExpressions;
using NewsReader.Core.Interfaces;

namespace NewsReader.Core.Decoding;

public sealed partial class Rfc2047Decoder : IRfc2047Decoder
{
    public string Decode(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return EncodedWordRegex().Replace(value, MatchEvaluator);
    }

    private static string MatchEvaluator(Match match)
    {
        var charset = match.Groups[1].Value;
        var encoding = match.Groups[2].Value;
        var payload = match.Groups[3].Value;

        try
        {
            var bytes = encoding.Equals("B", StringComparison.OrdinalIgnoreCase)
                ? Convert.FromBase64String(Pad(payload))
                : DecodeQuotedPrintable(payload);

            var textEncoding = ResolveEncoding(charset);
            return textEncoding.GetString(bytes);
        }
        catch (FormatException)
        {
            return match.Value;
        }
        catch (ArgumentException)
        {
            return match.Value;
        }
    }

    private static string Pad(string s)
    {
        var remainder = s.Length % 4;
        return remainder == 0 ? s : s.PadRight(s.Length + (4 - remainder), '=');
    }

    private static byte[] DecodeQuotedPrintable(string input)
    {
        using var ms = new MemoryStream();
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (c == '_')
            {
                ms.WriteByte((byte)' ');
                continue;
            }

            if (c == '=' && i + 2 < input.Length &&
                Uri.IsHexDigit(input[i + 1]) && Uri.IsHexDigit(input[i + 2]))
            {
                ms.WriteByte((byte)((Uri.FromHex(input[i + 1]) << 4) | Uri.FromHex(input[i + 2])));
                i += 2;
                continue;
            }

            ms.WriteByte((byte)c);
        }

        return ms.ToArray();
    }

    private static Encoding ResolveEncoding(string charset)
    {
        try
        {
            return Encoding.GetEncoding(charset);
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8;
        }
    }

    [GeneratedRegex(@"=\?([^?]+)\?([BbQq])\?([^?]*)\?=", RegexOptions.Compiled)]
    private static partial Regex EncodedWordRegex();
}
