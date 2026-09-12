using NUnit.Framework;
using NewsReader.Core.Decoding;
using NewsReader.Core.Interfaces;

namespace NewsReader.Core.Tests;

[TestFixture]
public class Rfc2047DecoderTests
{
    private IRfc2047Decoder _decoder = null!;

    [SetUp]
    public void SetUp()
    {
        _decoder = new Rfc2047Decoder();
    }

    [Test]
    public void Decodes_Base64_Utf8_Word()
    {
        var decoded = _decoder.Decode("=?UTF-8?B?SGVsbG8gV29ybGQ=?=");
        Assert.That(decoded, Is.EqualTo("Hello World"));
    }

    [Test]
    public void Decodes_QuotedPrintable_Utf8_Word()
    {
        var decoded = _decoder.Decode("=?UTF-8?Q?Caf=C3=A9?=");
        Assert.That(decoded, Is.EqualTo("Café"));
    }

    [Test]
    public void Decodes_Mixed_Text_And_EncodedWords()
    {
        var decoded = _decoder.Decode("Prefix =?UTF-8?B?SGVsbG8=?= suffix");
        Assert.That(decoded, Is.EqualTo("Prefix Hello suffix"));
    }

    [Test]
    public void Returns_Empty_For_Null()
    {
        var decoded = _decoder.Decode(null);
        Assert.That(decoded, Is.Empty);
    }

    [Test]
    public void Leaves_Invalid_Word_Unchanged()
    {
        var decoded = _decoder.Decode("=?UTF-8?B?not-base64!!?=");
        Assert.That(decoded, Is.EqualTo("=?UTF-8?B?not-base64!!?="));
    }
}
