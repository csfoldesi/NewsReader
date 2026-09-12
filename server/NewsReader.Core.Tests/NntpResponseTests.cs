using NewsReader.Core.Nntp;

namespace NewsReader.Core.Tests;

public class NntpResponseTests
{
    [Test]
    public void Parse_ExtractsCodeAndMessage()
    {
        var response = NntpResponse.Parse("200 NewsReader ready");

        Assert.That(response.Code, Is.EqualTo(200));
        Assert.That(response.Message, Is.EqualTo("NewsReader ready"));
    }

    [Test]
    public void Parse_HandlesCodeWithoutMessage()
    {
        var response = NntpResponse.Parse("200");

        Assert.That(response.Code, Is.EqualTo(200));
        Assert.That(response.Message, Is.Empty);
    }

    [Test]
    public void Parse_RejectsNonNumericLine()
    {
        Assert.Throws<FormatException>(() => NntpResponse.Parse("abc hello"));
    }

    [TestCase(200, true, false, false)]
    [TestCase(281, true, false, false)]
    [TestCase(381, false, true, false)]
    [TestCase(500, false, false, true)]
    public void Parse_ClassifiesResponse(int code, bool completion, bool intermediate, bool negative)
    {
        var response = new NntpResponse(code, "x");

        Assert.That(response.IsPositiveCompletion, Is.EqualTo(completion));
        Assert.That(response.IsPositiveIntermediate, Is.EqualTo(intermediate));
        Assert.That(response.IsNegative, Is.EqualTo(negative));
    }
}
