using System.Text;
using NewsReader.Core.Decoding;

namespace NewsReader.Core.Tests;

public class NntpTextReaderTests
{
    private static NntpTextReader CreateReader(string text) =>
        new(new MemoryStream(Encoding.UTF8.GetBytes(text)));

    [Test]
    public async Task ReadLineAsync_StripsCrLf()
    {
        await using var reader = CreateReader("200 hello\r\n");

        Assert.That(await reader.ReadLineAsync(), Is.EqualTo("200 hello"));
    }

    [Test]
    public async Task ReadLineAsync_ReturnsNullAtEof()
    {
        await using var reader = CreateReader(string.Empty);

        Assert.That(await reader.ReadLineAsync(), Is.Null);
    }

    [Test]
    public async Task ReadMultilineAsync_StopsAtDotAndUnstuffs()
    {
        await using var reader = CreateReader("line one\r\n..escaped\r\n.\r\nignored\r\n");

        var lines = new List<string>();
        await foreach (var line in reader.ReadMultilineAsync())
        {
            lines.Add(line);
        }

        Assert.That(lines, Is.EqualTo(new[] { "line one", ".escaped" }));
    }
}
