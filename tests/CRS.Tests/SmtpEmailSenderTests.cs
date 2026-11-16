using System.Net;
using System.Net.Sockets;
using System.Text;
using CRS.Core.Models;
using CRS.Core.Models.DTO;
using CRS.Core.Notifications;
using Microsoft.Extensions.Options;
using Xunit;

namespace CRS.Tests;

public sealed class SmtpEmailSenderTests
{
    [Theory]
    [InlineData(0, "live@example.test", "Test subject")]
    [InlineData(1, "debug@example.test", "[DEBUG] Test subject")]
    public async Task SendsOnlyToTheConfiguredRecipient(int mode, string expectedRecipient, string expectedSubject)
    {
        using var capture = new SmtpCapture();
        var received = capture.ReceiveAsync();
        var settings = new SmtpOptions
        {
            Host = "127.0.0.1", Port = capture.Port, From = "sender@example.test",
            // This local test server does not implement TLS. Production defaults to TLS enabled.
            EnableSsl = false, DebugEmail = mode, DebugEmailAddress = "debug@example.test"
        };
        var sender = new SmtpEmailSender(Options.Create(settings));
        await sender.SendAsync(new("live@example.test", "Test subject", "<p>Test body</p>"), default);
        var message = await received;
        Assert.Single(message.Recipients);
        Assert.Contains(expectedRecipient, message.Recipients[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Subject: " + expectedSubject, message.Data);
        Assert.Contains("<p>Test body</p>", message.Data);
        Assert.Contains("sender@example.test", message.From, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DisabledEmailDoesNotRequireOrContactSmtp()
    {
        var sender = new SmtpEmailSender(Options.Create(new SmtpOptions { DebugEmail = 2 }));
        await sender.SendAsync(new("not-an-address", "Test", "Test"), default);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public async Task InvalidModeFailsBeforeDelivery(int mode)
    {
        var sender = new SmtpEmailSender(Options.Create(new SmtpOptions { DebugEmail = mode }));
        await Assert.ThrowsAsync<DependencyException>(() => sender.SendAsync(new("live@example.test", "Test", "Test"), default));
    }

    [Fact]
    public async Task MissingDebugAddressDoesNotFallBackToLiveRecipient()
    {
        var sender = new SmtpEmailSender(Options.Create(new SmtpOptions { DebugEmail = 1 }));
        var error = await Assert.ThrowsAsync<DependencyException>(() => sender.SendAsync(new("live@example.test", "Test", "Test"), default));
        Assert.Contains("DebugEmailAddress", error.Message);
    }

    [Theory]
    [InlineData("sender@example.test", "")]
    [InlineData("", "test-password")]
    public async Task IncompleteCredentialsFailBeforeDelivery(string username, string password)
    {
        var sender = new SmtpEmailSender(Options.Create(new SmtpOptions
        {
            Host = "127.0.0.1", From = "sender@example.test", Username = username, Password = password
        }));
        var error = await Assert.ThrowsAsync<DependencyException>(() => sender.SendAsync(new("live@example.test", "Test", "Test"), default));
        Assert.Contains("both Smtp:Username and Smtp:Password", error.Message);
    }

    [Fact]
    public async Task UsesUsernameAsSenderAndAuthenticatesWithConfiguredCredentials()
    {
        using var capture = new SmtpCapture(authenticate: true);
        var received = capture.ReceiveAsync();
        var sender = new SmtpEmailSender(Options.Create(new SmtpOptions
        {
            Host = "127.0.0.1", Port = capture.Port, Username = "sender@example.test",
            Password = "test-password", EnableSsl = false
        }));
        await sender.SendAsync(new("live@example.test", "Test", "Test"), default);
        var message = await received;
        Assert.Equal("sender@example.test", message.Username);
        Assert.Equal("test-password", message.Password);
        Assert.Contains("sender@example.test", message.From, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record CapturedMessage(string From, List<string> Recipients, string Data, string? Username, string? Password);

    private sealed class SmtpCapture : IDisposable
    {
        private readonly TcpListener listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource timeout = new(TimeSpan.FromSeconds(15));
        private readonly bool authenticate;
        public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;
        public SmtpCapture(bool authenticate = false)
        {
            this.authenticate = authenticate;
            listener.Start();
        }
        public async Task<CapturedMessage> ReceiveAsync()
        {
            using var client = await listener.AcceptTcpClientAsync(timeout.Token);
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream);
            using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true, NewLine = "\r\n" };
            await writer.WriteLineAsync("220 localhost test SMTP");
            var from = "";
            var recipients = new List<string>();
            string? username = null, password = null;
            while (await reader.ReadLineAsync(timeout.Token) is { } command)
            {
                if (command.StartsWith("EHLO", StringComparison.OrdinalIgnoreCase))
                {
                    await writer.WriteLineAsync("250-localhost");
                    if (authenticate) await writer.WriteLineAsync("250-AUTH LOGIN");
                    await writer.WriteLineAsync("250 OK");
                }
                else if (command.StartsWith("AUTH LOGIN", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = command.Split(' ');
                    if (parts.Length > 2) username = Encoding.UTF8.GetString(Convert.FromBase64String(parts[2]));
                    else
                    {
                        await writer.WriteLineAsync("334 VXNlcm5hbWU6");
                        username = Encoding.UTF8.GetString(Convert.FromBase64String((await reader.ReadLineAsync(timeout.Token))!));
                    }
                    await writer.WriteLineAsync("334 UGFzc3dvcmQ6");
                    password = Encoding.UTF8.GetString(Convert.FromBase64String((await reader.ReadLineAsync(timeout.Token))!));
                    await writer.WriteLineAsync("235 Authentication successful");
                }
                else if (command.StartsWith("MAIL FROM:", StringComparison.OrdinalIgnoreCase))
                {
                    from = command;
                    await writer.WriteLineAsync("250 OK");
                }
                else if (command.StartsWith("RCPT TO:", StringComparison.OrdinalIgnoreCase))
                {
                    recipients.Add(command);
                    await writer.WriteLineAsync("250 OK");
                }
                else if (command == "DATA")
                {
                    await writer.WriteLineAsync("354 End with a period");
                    var data = new StringBuilder();
                    while (await reader.ReadLineAsync(timeout.Token) is { } line && line != ".") data.AppendLine(line);
                    await writer.WriteLineAsync("250 Queued");
                    return new(from, recipients, data.ToString(), username, password);
                }
                else throw new InvalidOperationException("Unexpected SMTP command in test capture.");
            }
            throw new InvalidOperationException("SMTP connection ended before a message was delivered.");
        }
        public void Dispose() { timeout.Cancel(); listener.Stop(); timeout.Dispose(); }
    }
}
