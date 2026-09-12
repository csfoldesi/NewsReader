namespace NewsReader.Core.Interfaces;

public interface IRfc2047Decoder
{
    string Decode(string? value);
}
