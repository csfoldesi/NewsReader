using Moq;
using NewsReader.Core.Interfaces;
using NewsReader.Core.Nntp;

namespace NewsReader.Core.Tests;

public class NntpClientTests
{
    [Test]
    public async Task ConnectAsync_WithoutCredentials_DoesNotAuthenticate()
    {
        var connection = new Mock<INntpConnection>();
        connection.SetupGet(c => c.IsConnected).Returns(true);
        connection.Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NntpResponse(200, "ready"));

        await using var client = new NntpClient(connection.Object, new NntpConnectionOptions { Host = "example" });

        var banner = await client.ConnectAsync();

        Assert.That(banner.Code, Is.EqualTo(200));
        Assert.That(client.Banner, Is.SameAs(banner));
        connection.Verify(c => c.WriteLineAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ConnectAsync_WithUsername_Authenticates()
    {
        var connection = new Mock<INntpConnection>();
        connection.SetupGet(c => c.IsConnected).Returns(true);
        connection.Setup(c => c.ConnectAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NntpResponse(200, "ready"));
        connection.Setup(c => c.ReadResponseAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NntpResponse(281, "auth ok"));

        await using var client = new NntpClient(
            connection.Object,
            new NntpConnectionOptions { Host = "example", Username = "alice", Password = "secret" });

        await client.ConnectAsync();

        connection.Verify(
            c => c.WriteLineAsync("AUTHINFO USER alice", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AuthenticateAsync_ImmediateSuccess_SendsUserOnly()
    {
        var connection = new Mock<INntpConnection>();
        connection.Setup(c => c.ReadResponseAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NntpResponse(281, "auth ok"));

        await using var client = new NntpClient(connection.Object);

        await client.AuthenticateAsync("alice", "secret");

        connection.Verify(
            c => c.WriteLineAsync("AUTHINFO USER alice", It.IsAny<CancellationToken>()),
            Times.Once);
        connection.Verify(
            c => c.WriteLineAsync(It.Is<string>(s => s.StartsWith("AUTHINFO PASS")), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task AuthenticateAsync_PasswordRequired_SendsUserThenPass()
    {
        var connection = new Mock<INntpConnection>();
        connection.SetupSequence(c => c.ReadResponseAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NntpResponse(381, "password required"))
            .ReturnsAsync(new NntpResponse(281, "auth ok"));

        await using var client = new NntpClient(connection.Object);

        await client.AuthenticateAsync("alice", "secret");

        connection.Verify(
            c => c.WriteLineAsync("AUTHINFO USER alice", It.IsAny<CancellationToken>()),
            Times.Once);
        connection.Verify(
            c => c.WriteLineAsync("AUTHINFO PASS secret", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task AuthenticateAsync_RejectedUser_Throws()
    {
        var connection = new Mock<INntpConnection>();
        connection.Setup(c => c.ReadResponseAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NntpResponse(502, "no access"));

        await using var client = new NntpClient(connection.Object);

        var ex = Assert.ThrowsAsync<NntpException>(() => client.AuthenticateAsync("alice", "secret"));

        Assert.That(ex!.Code, Is.EqualTo(502));
        connection.Verify(
            c => c.WriteLineAsync("AUTHINFO USER alice", It.IsAny<CancellationToken>()),
            Times.Once);
        connection.Verify(
            c => c.WriteLineAsync(It.Is<string>(s => s.StartsWith("AUTHINFO PASS")), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task AuthenticateAsync_RejectedPassword_Throws()
    {
        var connection = new Mock<INntpConnection>();
        connection.SetupSequence(c => c.ReadResponseAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NntpResponse(381, "password required"))
            .ReturnsAsync(new NntpResponse(481, "bad password"));

        await using var client = new NntpClient(connection.Object);

        var ex = Assert.ThrowsAsync<NntpException>(() => client.AuthenticateAsync("alice", "wrong"));

        Assert.That(ex!.Code, Is.EqualTo(481));
        connection.Verify(
            c => c.WriteLineAsync("AUTHINFO USER alice", It.IsAny<CancellationToken>()),
            Times.Once);
        connection.Verify(
            c => c.WriteLineAsync("AUTHINFO PASS wrong", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
