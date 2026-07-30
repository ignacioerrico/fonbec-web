using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Logic.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Fonbec.Web.Logic.Tests.Services;

public class DocumentNotificationServiceTests
{
    private readonly IDocumentRepository _documentRepository = Substitute.For<IDocumentRepository>();
    private readonly IEmailMessageSender _emailMessageSender = Substitute.For<IEmailMessageSender>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();

    public DocumentNotificationServiceTests()
    {
        _configuration["App:BaseUrl"].Returns("https://fonbec.test");
    }

    private DocumentNotificationService CreateService() =>
        new(_documentRepository, _emailMessageSender, _configuration, NullLogger<DocumentNotificationService>.Instance);

    [Fact]
    public async Task NotifySponsorsAsync_Sends_Email_With_Personalized_Content()
    {
        var token = Guid.NewGuid();
        _documentRepository.GetUnnotifiedSharesAsync(42).Returns(
        [
            new DocumentShareNotificationDataModel
            {
                DocumentShareId = 1,
                RecipientEmail = "padrino@test.com",
                RecipientName = "Juan",
                RecipientNickName = "Juancito",
                PublicAccessToken = token,
                StudentId = 7,
                StudentFirstName = "María",
                StudentLastName = "García",
                StudentNickName = "Mari",
                StudentGender = Gender.Female,
            },
        ]);

        await CreateService().NotifySponsorsAsync(42);

        await _emailMessageSender.Received(1).SendEmailAsync(
            "padrino@test.com",
            "Nuevo documento disponible",
            Arg.Is<string>(html =>
                html.Contains("Hola, Juancito:")
                && html.Contains("de tu ahijada Mari García.")
                && html.Contains($"https://fonbec.test/padrinos/{token}/7")));

        await _documentRepository.Received(1).MarkShareNotifiedAsync(1, Arg.Any<DateTime>());
    }

    [Fact]
    public async Task NotifySponsorsAsync_Sends_Company_Email_With_Company_History_Link()
    {
        var token = Guid.NewGuid();
        _documentRepository.GetUnnotifiedSharesAsync(42).Returns(
        [
            new DocumentShareNotificationDataModel
            {
                DocumentShareId = 5,
                IsCompany = true,
                RecipientEmail = "empresa@test.com",
                RecipientName = "Acme SA",
                PublicAccessToken = token,
                StudentId = 7,
                StudentFirstName = "María",
                StudentLastName = "García",
                StudentNickName = "Mari",
                StudentGender = Gender.Female,
            },
        ]);

        await CreateService().NotifySponsorsAsync(42);

        await _emailMessageSender.Received(1).SendEmailAsync(
            "empresa@test.com",
            "Nuevo documento disponible",
            Arg.Is<string>(html =>
                html.Contains("Hola, Acme SA:")
                && html.Contains($"https://fonbec.test/empresas/{token}/7")));

        await _documentRepository.Received(1).MarkShareNotifiedAsync(5, Arg.Any<DateTime>());
    }

    [Fact]
    public async Task NotifySponsorsAsync_Skips_Send_But_Marks_Notified_When_Recipient_Has_No_Email()
    {
        _documentRepository.GetUnnotifiedSharesAsync(42).Returns(
        [
            new DocumentShareNotificationDataModel
            {
                DocumentShareId = 9,
                IsCompany = true,
                RecipientEmail = string.Empty,
                RecipientName = "Sin Email SA",
                PublicAccessToken = Guid.NewGuid(),
                StudentId = 7,
                StudentFirstName = "María",
                StudentLastName = "García",
                StudentGender = Gender.Female,
            },
        ]);

        await CreateService().NotifySponsorsAsync(42);

        await _emailMessageSender.DidNotReceive().SendEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await _documentRepository.Received(1).MarkShareNotifiedAsync(9, Arg.Any<DateTime>());
    }

    [Fact]
    public async Task NotifySponsorsAsync_Retries_And_Succeeds_On_Second_Attempt()
    {
        _documentRepository.GetUnnotifiedSharesAsync(42).Returns(
        [
            new DocumentShareNotificationDataModel
            {
                DocumentShareId = 3,
                RecipientEmail = "retry@test.com",
                RecipientName = "Juan",
                PublicAccessToken = Guid.NewGuid(),
                StudentId = 7,
                StudentFirstName = "María",
                StudentLastName = "García",
                StudentGender = Gender.Female,
            },
        ]);

        _emailMessageSender
            .SendEmailAsync("retry@test.com", Arg.Any<string>(), Arg.Any<string>())
            .Returns(
                _ => throw new InvalidOperationException("transient"),
                _ => Task.CompletedTask);

        await CreateService().NotifySponsorsAsync(42);

        await _emailMessageSender.Received(2).SendEmailAsync(
            "retry@test.com", Arg.Any<string>(), Arg.Any<string>());
        await _documentRepository.Received(1).MarkShareNotifiedAsync(3, Arg.Any<DateTime>());
    }

    [Fact]
    public async Task NotifySponsorsAsync_Leaves_Share_Unmarked_After_Exhausted_Retries()
    {
        _documentRepository.GetUnnotifiedSharesAsync(42).Returns(
        [
            new DocumentShareNotificationDataModel
            {
                DocumentShareId = 4,
                RecipientEmail = "fail@test.com",
                RecipientName = "Juan",
                PublicAccessToken = Guid.NewGuid(),
                StudentId = 7,
                StudentFirstName = "María",
                StudentLastName = "García",
                StudentGender = Gender.Female,
            },
        ]);

        _emailMessageSender
            .SendEmailAsync("fail@test.com", Arg.Any<string>(), Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException("permanent"));

        await CreateService().NotifySponsorsAsync(42);

        await _emailMessageSender.Received(3).SendEmailAsync(
            "fail@test.com", Arg.Any<string>(), Arg.Any<string>());
        await _documentRepository.DidNotReceive().MarkShareNotifiedAsync(4, Arg.Any<DateTime>());
    }

    [Fact]
    public async Task NotifySponsorsAsync_Continues_Notifying_Remaining_Shares_After_Failure()
    {
        _documentRepository.GetUnnotifiedSharesAsync(42).Returns(
        [
            new DocumentShareNotificationDataModel
            {
                DocumentShareId = 10,
                RecipientEmail = "fail@test.com",
                RecipientName = "Fallo",
                PublicAccessToken = Guid.NewGuid(),
                StudentId = 7,
                StudentFirstName = "María",
                StudentLastName = "García",
                StudentGender = Gender.Female,
            },
            new DocumentShareNotificationDataModel
            {
                DocumentShareId = 11,
                RecipientEmail = "ok@test.com",
                RecipientName = "Ok",
                PublicAccessToken = Guid.NewGuid(),
                StudentId = 7,
                StudentFirstName = "María",
                StudentLastName = "García",
                StudentGender = Gender.Female,
            },
        ]);

        _emailMessageSender
            .SendEmailAsync("fail@test.com", Arg.Any<string>(), Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException("permanent"));

        await CreateService().NotifySponsorsAsync(42);

        await _emailMessageSender.Received(3).SendEmailAsync(
            "fail@test.com", Arg.Any<string>(), Arg.Any<string>());
        await _emailMessageSender.Received(1).SendEmailAsync(
            "ok@test.com", Arg.Any<string>(), Arg.Any<string>());
        await _documentRepository.DidNotReceive().MarkShareNotifiedAsync(10, Arg.Any<DateTime>());
        await _documentRepository.Received(1).MarkShareNotifiedAsync(11, Arg.Any<DateTime>());
    }
}
