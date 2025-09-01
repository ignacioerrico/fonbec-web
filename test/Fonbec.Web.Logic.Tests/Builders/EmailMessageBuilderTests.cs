using FluentAssertions;
using FluentAssertions.Execution;
using Fonbec.Web.Logic.Builders;
using Fonbec.Web.Logic.Models;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Builders;

public class EmailMessageBuilderTests
{
    private readonly List<Recipient> _to;
    private readonly string _subject;
    private readonly string _htmlBody;

    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();

    public EmailMessageBuilderTests()
    {
        _to = [new("john@doe.com", "John Doe")];
        _subject = "Sample email";
        _htmlBody = "Sample message";

        var configurationSection = Substitute.For<IConfigurationSection>();
        configurationSection.Value.Returns((string?)null);

        _configuration
            .GetSection(Arg.Any<string>())
            .Returns(configurationSection);
    }

    [Fact]
    public void ShouldThrowNullReferenceException_WhenEmailFromNotSet()
    {
        // Configuration will return null when trying to get the section Email:From
        var emailMessageBuilder =
            () => new EmailMessageBuilder(_configuration, _to, _subject, _htmlBody);

        emailMessageBuilder.Should().Throw<NullReferenceException>()
            .WithMessage("Email:From not set.");
    }

    [Fact]
    public void ShouldOnlySet_SenderAddress_RecipientsTo_ContentsSubject_ContentsHtml_When_EmailFrom_IsSet_AndNoPropertyIsSetExplicitely()
    {
        // Arrange
        var configurationSection = Substitute.For<IConfigurationSection>();
        configurationSection.Value.Returns("from@email.com");

        _configuration
            .GetSection("Email:From")
            .Returns(configurationSection);

        var emailMessageBuilder = new EmailMessageBuilder(_configuration, _to, _subject, _htmlBody);

        // Act
        var emailMessage = emailMessageBuilder.Build();

        // Assert
        using (new AssertionScope())
        {
            emailMessage.SenderAddress.Should().Be("from@email.com");

            emailMessage.Recipients.To.Should().HaveCount(1);
            emailMessage.Recipients.To.Single().Address.Should().Be("<john@doe.com>");
            emailMessage.Recipients.To.Single().DisplayName.Should().Be("\"John Doe\"");

            emailMessage.Content.Subject.Should().Be("Sample email");

            emailMessage.Content.Html.Should().Be("Sample message");

            // The rest of the properties should be unset.
            emailMessage.Content.PlainText.Should().BeNull();

            emailMessage.Recipients.CC.Should().BeEmpty();
            emailMessage.Recipients.BCC.Should().BeEmpty();
            emailMessage.Headers.Should().BeEmpty();
            emailMessage.ReplyTo.Should().BeEmpty();
            emailMessage.Attachments.Should().BeEmpty();
        }
    }

    [Fact]
    public void ShouldSet_ReplyTo_WithDisplayNameSetToEmail_WhenOnly_EmailReplyToEmail_IsSet()
    {
        // Arrange
        var configurationSectionEmailFrom = Substitute.For<IConfigurationSection>();
        configurationSectionEmailFrom.Value.Returns("from@email.com");

        _configuration
            .GetSection("Email:From")
            .Returns(configurationSectionEmailFrom);

        var configurationSectionEmailReplyToEmail = Substitute.For<IConfigurationSection>();
        configurationSectionEmailReplyToEmail.Value.Returns("reply-to@email.com");

        _configuration
            .GetSection("Email:ReplyToEmail")
            .Returns(configurationSectionEmailReplyToEmail);

        var emailMessageBuilder = new EmailMessageBuilder(_configuration, _to, _subject, _htmlBody);

        // Act
        var emailMessage = emailMessageBuilder.Build();

        // Assert
        using (new AssertionScope())
        {
            emailMessage.SenderAddress.Should().Be("from@email.com");

            emailMessage.Recipients.To.Should().HaveCount(1);
            emailMessage.Recipients.To.Single().Address.Should().Be("<john@doe.com>");
            emailMessage.Recipients.To.Single().DisplayName.Should().Be("\"John Doe\"");

            emailMessage.ReplyTo.Should().HaveCount(1);
            emailMessage.ReplyTo.Single().Address.Should().Be("<reply-to@email.com>");
            emailMessage.ReplyTo.Single().DisplayName.Should().Be("reply-to@email.com");

            emailMessage.Content.Subject.Should().Be("Sample email");

            emailMessage.Content.Html.Should().Be("Sample message");

            // The rest of the properties should be unset.
            emailMessage.Content.PlainText.Should().BeNull();

            emailMessage.Recipients.CC.Should().BeEmpty();
            emailMessage.Recipients.BCC.Should().BeEmpty();
            emailMessage.Headers.Should().BeEmpty();
            emailMessage.Attachments.Should().BeEmpty();
        }
    }

    [Fact]
    public void ShouldSet_ReplyTo_When_EmailReplyToEmail_EmailReplyToDisplayName_AreSet()
    {
        // Arrange
        var configurationSectionEmailFrom = Substitute.For<IConfigurationSection>();
        configurationSectionEmailFrom.Value.Returns("from@email.com");

        _configuration
            .GetSection("Email:From")
            .Returns(configurationSectionEmailFrom);

        var configurationSectionEmailReplyToEmail = Substitute.For<IConfigurationSection>();
        configurationSectionEmailReplyToEmail.Value.Returns("reply-to@email.com");

        _configuration
            .GetSection("Email:ReplyToEmail")
            .Returns(configurationSectionEmailReplyToEmail);

        var configurationSectionEmailReplyToDisplayName = Substitute.For<IConfigurationSection>();
        configurationSectionEmailReplyToDisplayName.Value.Returns("ReplyTo Display Name");

        _configuration
            .GetSection("Email:ReplyToDisplayName")
            .Returns(configurationSectionEmailReplyToDisplayName);

        var emailMessageBuilder = new EmailMessageBuilder(_configuration, _to, _subject, _htmlBody);

        // Act
        var emailMessage = emailMessageBuilder.Build();

        // Assert
        using (new AssertionScope())
        {
            emailMessage.SenderAddress.Should().Be("from@email.com");

            emailMessage.Recipients.To.Should().HaveCount(1);
            emailMessage.Recipients.To.Single().Address.Should().Be("<john@doe.com>");
            emailMessage.Recipients.To.Single().DisplayName.Should().Be("\"John Doe\"");

            emailMessage.ReplyTo.Should().HaveCount(1);
            emailMessage.ReplyTo.Single().Address.Should().Be("<reply-to@email.com>");
            emailMessage.ReplyTo.Single().DisplayName.Should().Be("\"ReplyTo Display Name\"");

            emailMessage.Content.Subject.Should().Be("Sample email");

            emailMessage.Content.Html.Should().Be("Sample message");

            // The rest of the properties should be unset.
            emailMessage.Content.PlainText.Should().BeNull();

            emailMessage.Recipients.CC.Should().BeEmpty();
            emailMessage.Recipients.BCC.Should().BeEmpty();
            emailMessage.Headers.Should().BeEmpty();
            emailMessage.Attachments.Should().BeEmpty();
        }
    }

    [Fact]
    public void ShouldSet_Recipients_Cc_Bcc_When_Cc_Bcc_AreSet()
    {
        // Arrange
        var cc = new List<Recipient>
            {
                new("first-cc@email.com", "First Cc"),
                new("second-cc@email.com")
            };
        var bcc = new List<Recipient>()
            {
                new("first-bcc@email.com"),
                new("second-bcc@email.com", "Second Bcc")
            };

        var configurationSectionEmailFrom = Substitute.For<IConfigurationSection>();
        configurationSectionEmailFrom.Value.Returns("from@email.com");

        _configuration
            .GetSection("Email:From")
            .Returns(configurationSectionEmailFrom);

        var emailMessageBuilder = new EmailMessageBuilder(_configuration, _to, _subject, _htmlBody);
        emailMessageBuilder.Cc.AddRange(cc);
        emailMessageBuilder.Bcc.AddRange(bcc);

        // Act
        var emailMessage = emailMessageBuilder.Build();

        // Assert
        using (new AssertionScope())
        {
            emailMessage.SenderAddress.Should().Be("from@email.com");

            emailMessage.Recipients.To.Should().HaveCount(1);
            emailMessage.Recipients.To.Single().Address.Should().Be("<john@doe.com>");
            emailMessage.Recipients.To.Single().DisplayName.Should().Be("\"John Doe\"");

            emailMessage.Recipients.CC.Should().HaveCount(2);
            emailMessage.Recipients.CC[0].Address.Should().Be("<first-cc@email.com>");
            emailMessage.Recipients.CC[0].DisplayName.Should().Be("\"First Cc\"");
            emailMessage.Recipients.CC[1].Address.Should().Be("<second-cc@email.com>");
            emailMessage.Recipients.CC[1].DisplayName.Should().Be("second-cc@email.com");

            emailMessage.Recipients.BCC.Should().HaveCount(2);
            emailMessage.Recipients.BCC[0].Address.Should().Be("<first-bcc@email.com>");
            emailMessage.Recipients.BCC[0].DisplayName.Should().Be("first-bcc@email.com");
            emailMessage.Recipients.BCC[1].Address.Should().Be("<second-bcc@email.com>");
            emailMessage.Recipients.BCC[1].DisplayName.Should().Be("\"Second Bcc\"");

            emailMessage.Content.Subject.Should().Be("Sample email");

            emailMessage.Content.Html.Should().Be("Sample message");

            // The rest of the properties should be unset.
            emailMessage.Content.PlainText.Should().BeNull();

            emailMessage.Headers.Should().BeEmpty();
            emailMessage.ReplyTo.Should().BeEmpty();
            emailMessage.Attachments.Should().BeEmpty();
        }
    }

    [Fact]
    public void ShouldSet_ContentsPlainText_When_BodyPlainText_IsSet()
    {
        // Arrange
        var configurationSectionEmailFrom = Substitute.For<IConfigurationSection>();
        configurationSectionEmailFrom.Value.Returns("from@email.com");

        _configuration
            .GetSection("Email:From")
            .Returns(configurationSectionEmailFrom);

        var emailMessageBuilder = new EmailMessageBuilder(_configuration, _to, _subject, _htmlBody)
        {
            BodyPlainText = "Message in plain text"
        };

        // Act
        var emailMessage = emailMessageBuilder.Build();

        // Assert
        using (new AssertionScope())
        {
            emailMessage.SenderAddress.Should().Be("from@email.com");

            emailMessage.Recipients.To.Should().HaveCount(1);
            emailMessage.Recipients.To.Single().Address.Should().Be("<john@doe.com>");
            emailMessage.Recipients.To.Single().DisplayName.Should().Be("\"John Doe\"");

            emailMessage.Content.Subject.Should().Be("Sample email");

            emailMessage.Content.Html.Should().Be("Sample message");

            emailMessage.Content.PlainText.Should().Be("Message in plain text");

            // The rest of the properties should be unset.
            emailMessage.Recipients.CC.Should().BeEmpty();
            emailMessage.Recipients.BCC.Should().BeEmpty();
            emailMessage.Headers.Should().BeEmpty();
            emailMessage.ReplyTo.Should().BeEmpty();
            emailMessage.Attachments.Should().BeEmpty();
        }
    }
}