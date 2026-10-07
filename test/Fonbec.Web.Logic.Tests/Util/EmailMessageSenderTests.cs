using Azure;
using Azure.Communication.Email;
using FluentAssertions;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Util;

public class EmailMessageSenderTests
{
    [Fact]
    public async Task SendEmailAsync_SingleAddress_Sends_One_To_And_No_Copies()
    {
        var (sender, sent) = CreateSender();

        await sender.SendEmailAsync("ana@ejemplo.com", "Asunto", "<p>Hola</p>");

        var message = sent.Should().ContainSingle().Subject;
        message.Recipients.To.Should().ContainSingle().Which.Address.Should().Be("<ana@ejemplo.com>");
        message.Recipients.CC.Should().BeEmpty();
        message.Recipients.BCC.Should().BeEmpty();
        message.Attachments.Should().BeEmpty();
        message.Content.Subject.Should().Be("Asunto");
        message.Content.Html.Should().Be("<p>Hola</p>");
    }

    [Fact]
    public async Task SendEmailAsync_Passes_Cc_And_Bcc_Without_Attachments()
    {
        var (sender, sent) = CreateSender();

        await sender.SendEmailAsync(
            [new Recipient("ana@ejemplo.com")],
            [new Recipient("luis@ejemplo.com", "Luis Perez")],
            [new Recipient("marta@ejemplo.com", "Marta Gomez")],
            "Nuevo documento disponible",
            "<p>Ver historial</p>");

        var message = sent.Should().ContainSingle().Subject;
        message.Recipients.To.Should().ContainSingle().Which.Address.Should().Be("<ana@ejemplo.com>");
        message.Recipients.CC.Should().ContainSingle().Which.Address.Should().Be("<luis@ejemplo.com>");
        message.Recipients.CC.Single().DisplayName.Should().Be("\"Luis Perez\"");
        message.Recipients.BCC.Should().ContainSingle().Which.Address.Should().Be("<marta@ejemplo.com>");
        message.Recipients.BCC.Single().DisplayName.Should().Be("\"Marta Gomez\"");
        message.Attachments.Should().BeEmpty();
    }

    private static (EmailMessageSender Sender, List<EmailMessage> Sent) CreateSender()
    {
        var configuration = Substitute.For<IConfiguration>();
        var from = Substitute.For<IConfigurationSection>();
        from.Value.Returns("from@fonbec.test");
        var empty = Substitute.For<IConfigurationSection>();
        empty.Value.Returns((string?)null);
        configuration.GetSection("Email:From").Returns(from);
        configuration.GetSection(Arg.Is<string>(key => key != "Email:From")).Returns(empty);

        var sent = new List<EmailMessage>();

        // The access key is the base64 encoding of the word "test".
        // The constructor requires the access key to be valid base64.
        var client = Substitute.For<EmailClient>(
            "endpoint=https://contoso.communication.azure.com/;accesskey=dGVzdA==");
        client
            .SendAsync(Arg.Any<WaitUntil>(), Arg.Do<EmailMessage>(sent.Add), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EmailSendOperation>(null!));

        var sender = new EmailMessageSender(configuration, NullLogger<EmailMessageSender>.Instance, client);
        return (sender, sent);
    }
}