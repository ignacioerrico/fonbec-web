using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.DataModels.Users;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models;
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
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailMessageSender _emailMessageSender = Substitute.For<IEmailMessageSender>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();

    public DocumentNotificationServiceTests()
    {
        _configuration["App:BaseUrl"].Returns("https://fonbec.test");
    }

    private DocumentNotificationService CreateService() =>
        new(_documentRepository, _userRepository, _emailMessageSender, _configuration, NullLogger<DocumentNotificationService>.Instance);

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

        await CreateService().NotifySponsorsAsync(42, TestContext.Current.CancellationToken);

        await _emailMessageSender.Received(1).SendEmailAsync(
            Arg.Is<IReadOnlyList<Recipient>>(to => Matches(to, new Recipient("padrino@test.com"))),
            Arg.Is<IReadOnlyList<Recipient>>(cc => cc.Count == 0),
            Arg.Is<IReadOnlyList<Recipient>>(bcc => bcc.Count == 0),
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

        await CreateService().NotifySponsorsAsync(42, TestContext.Current.CancellationToken);

        await _emailMessageSender.Received(1).SendEmailAsync(
            Arg.Is<IReadOnlyList<Recipient>>(to => Matches(to, new Recipient("empresa@test.com"))),
            Arg.Is<IReadOnlyList<Recipient>>(cc => cc.Count == 0),
            Arg.Is<IReadOnlyList<Recipient>>(bcc => bcc.Count == 0),
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

        await CreateService().NotifySponsorsAsync(42, TestContext.Current.CancellationToken);

        await _emailMessageSender.DidNotReceive().SendEmailAsync(
            Arg.Any<IReadOnlyList<Recipient>>(),
            Arg.Any<IReadOnlyList<Recipient>>(),
            Arg.Any<IReadOnlyList<Recipient>>(),
            Arg.Any<string>(),
            Arg.Any<string>());
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
            .SendEmailAsync(
                Arg.Is<IReadOnlyList<Recipient>>(to => Matches(to, new Recipient("retry@test.com"))),
                Arg.Any<IReadOnlyList<Recipient>>(),
                Arg.Any<IReadOnlyList<Recipient>>(),
                Arg.Any<string>(),
                Arg.Any<string>())
            .Returns(
                _ => throw new InvalidOperationException("transient"),
                _ => Task.CompletedTask);

        await CreateService().NotifySponsorsAsync(42, TestContext.Current.CancellationToken);

        await _emailMessageSender.Received(2).SendEmailAsync(
            Arg.Is<IReadOnlyList<Recipient>>(to => Matches(to, new Recipient("retry@test.com"))),
            Arg.Any<IReadOnlyList<Recipient>>(),
            Arg.Any<IReadOnlyList<Recipient>>(),
            Arg.Any<string>(),
            Arg.Any<string>());
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
            .SendEmailAsync(
                Arg.Is<IReadOnlyList<Recipient>>(to => Matches(to, new Recipient("fail@test.com"))),
                Arg.Any<IReadOnlyList<Recipient>>(),
                Arg.Any<IReadOnlyList<Recipient>>(),
                Arg.Any<string>(),
                Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException("permanent"));

        await CreateService().NotifySponsorsAsync(42, TestContext.Current.CancellationToken);

        await _emailMessageSender.Received(3).SendEmailAsync(
            Arg.Is<IReadOnlyList<Recipient>>(to => Matches(to, new Recipient("fail@test.com"))),
            Arg.Any<IReadOnlyList<Recipient>>(),
            Arg.Any<IReadOnlyList<Recipient>>(),
            Arg.Any<string>(),
            Arg.Any<string>());
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
            .SendEmailAsync(
                Arg.Is<IReadOnlyList<Recipient>>(to => Matches(to, new Recipient("fail@test.com"))),
                Arg.Any<IReadOnlyList<Recipient>>(),
                Arg.Any<IReadOnlyList<Recipient>>(),
                Arg.Any<string>(),
                Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException("permanent"));

        await CreateService().NotifySponsorsAsync(42, TestContext.Current.CancellationToken);

        await _emailMessageSender.Received(3).SendEmailAsync(
            Arg.Is<IReadOnlyList<Recipient>>(to => Matches(to, new Recipient("fail@test.com"))),
            Arg.Any<IReadOnlyList<Recipient>>(),
            Arg.Any<IReadOnlyList<Recipient>>(),
            Arg.Any<string>(),
            Arg.Any<string>());
        await _emailMessageSender.Received(1).SendEmailAsync(
            Arg.Is<IReadOnlyList<Recipient>>(to => Matches(to, new Recipient("ok@test.com"))),
            Arg.Any<IReadOnlyList<Recipient>>(),
            Arg.Any<IReadOnlyList<Recipient>>(),
            Arg.Any<string>(),
            Arg.Any<string>());
        await _documentRepository.DidNotReceive().MarkShareNotifiedAsync(10, Arg.Any<DateTime>());
        await _documentRepository.Received(1).MarkShareNotifiedAsync(11, Arg.Any<DateTime>());
    }

    [Fact]
    public async Task NotifyChapterManagersPlanReadyAsync_Emails_Managers_With_Progress_Link()
    {
        _userRepository.GetChapterManagerContactsAsync(3).Returns(
        [
            new ChapterManagerContactDataModel("coord@test.com", "Ana Coordinadora"),
        ]);

        await CreateService().NotifyChapterManagersPlanReadyAsync(
            3, 88, new DateTime(2026, 9, 1), TestContext.Current.CancellationToken);

        await _emailMessageSender.Received(1).SendEmailAsync(
            "coord@test.com",
            "Campaña lista para completar",
            Arg.Is<string>(html =>
                html.Contains("septiembre de 2026")
                && html.Contains("https://fonbec.test/planificaciones/88/cartas")));
    }

    [Fact]
    public async Task NotifyChapterManagersPlanReadyAsync_Does_Nothing_When_No_Managers()
    {
        _userRepository.GetChapterManagerContactsAsync(3)
            .Returns(Array.Empty<ChapterManagerContactDataModel>());

        await CreateService().NotifyChapterManagersPlanReadyAsync(
            3, 88, new DateTime(2026, 9, 1), TestContext.Current.CancellationToken);

        await _emailMessageSender.DidNotReceiveWithAnyArgs()
            .SendEmailAsync(default!, default!, default!);
        await _emailMessageSender.DidNotReceiveWithAnyArgs()
            .SendEmailAsync(default!, default!, default!, default!, default!);
    }

    [Fact]
    public async Task NotifySponsorsAsync_Copies_Person_SendAlsoTo_As_Cc_And_Bcc()
    {
        var token = Guid.NewGuid();
        _documentRepository.GetUnnotifiedSharesAsync(42).Returns(
        [
            new DocumentShareNotificationDataModel
            {
                DocumentShareId = 1,
                RecipientEmail = "ana@ejemplo.com",
                RecipientName = "Ana",
                PublicAccessToken = token,
                StudentId = 7,
                StudentFirstName = "María",
                StudentLastName = "García",
                StudentGender = Gender.Female,
                AdditionalRecipients =
                [
                    new SendAlsoToNotificationDataModel
                    {
                        RecipientName = "Luis Perez",
                        RecipientEmail = "luis@ejemplo.com",
                        SendAsBcc = false,
                    },
                    new SendAlsoToNotificationDataModel
                    {
                        RecipientName = "Pedro Diaz",
                        RecipientEmail = "pedro@ejemplo.com",
                        SendAsBcc = false,
                    },
                    new SendAlsoToNotificationDataModel
                    {
                        RecipientName = "Marta Gomez",
                        RecipientEmail = "marta@ejemplo.com",
                        SendAsBcc = true,
                    },
                ],
            },
        ]);

        await CreateService().NotifySponsorsAsync(42, TestContext.Current.CancellationToken);

        await _emailMessageSender.Received(1).SendEmailAsync(
            Arg.Is<IReadOnlyList<Recipient>>(to => Matches(to, new Recipient("ana@ejemplo.com"))),
            Arg.Is<IReadOnlyList<Recipient>>(cc => Matches(
                cc,
                new Recipient("luis@ejemplo.com", "Luis Perez"),
                new Recipient("pedro@ejemplo.com", "Pedro Diaz"))),
            Arg.Is<IReadOnlyList<Recipient>>(bcc => Matches(bcc, new Recipient("marta@ejemplo.com", "Marta Gomez"))),
            "Nuevo documento disponible",
            Arg.Is<string>(html =>
                html.Contains($"https://fonbec.test/padrinos/{token}/7")
                && !html.Contains("luis@ejemplo.com")
                && !html.Contains("marta@ejemplo.com")));

        await _documentRepository.Received(1).MarkShareNotifiedAsync(1, Arg.Any<DateTime>());
    }

    [Fact]
    public async Task NotifySponsorsAsync_Keeps_Company_Email_To_Only_Even_If_Extra_Recipients_Are_Present()
    {
        _documentRepository.GetUnnotifiedSharesAsync(42).Returns(
        [
            new DocumentShareNotificationDataModel
            {
                DocumentShareId = 5,
                IsCompany = true,
                RecipientEmail = "empresa@test.com",
                RecipientName = "Acme SA",
                PublicAccessToken = Guid.NewGuid(),
                StudentId = 7,
                StudentFirstName = "María",
                StudentLastName = "García",
                StudentGender = Gender.Female,
                AdditionalRecipients =
                [
                    new SendAlsoToNotificationDataModel
                    {
                        RecipientName = "Luis Perez",
                        RecipientEmail = "luis@ejemplo.com",
                        SendAsBcc = false,
                    },
                ],
            },
        ]);

        await CreateService().NotifySponsorsAsync(42, TestContext.Current.CancellationToken);

        await _emailMessageSender.Received(1).SendEmailAsync(
            Arg.Is<IReadOnlyList<Recipient>>(to => Matches(to, new Recipient("empresa@test.com"))),
            Arg.Is<IReadOnlyList<Recipient>>(cc => cc.Count == 0),
            Arg.Is<IReadOnlyList<Recipient>>(bcc => bcc.Count == 0),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task NotifySponsorsAsync_Does_Not_Promote_Cc_When_Sponsor_Has_No_Email()
    {
        _documentRepository.GetUnnotifiedSharesAsync(42).Returns(
        [
            new DocumentShareNotificationDataModel
            {
                DocumentShareId = 9,
                RecipientEmail = "  ",
                RecipientName = "Ana",
                PublicAccessToken = Guid.NewGuid(),
                StudentId = 7,
                StudentFirstName = "María",
                StudentLastName = "García",
                StudentGender = Gender.Female,
                AdditionalRecipients =
                [
                    new SendAlsoToNotificationDataModel
                    {
                        RecipientName = "Luis Perez",
                        RecipientEmail = "luis@ejemplo.com",
                        SendAsBcc = false,
                    },
                ],
            },
        ]);

        await CreateService().NotifySponsorsAsync(42, TestContext.Current.CancellationToken);

        await _emailMessageSender.DidNotReceive().SendEmailAsync(
            Arg.Any<IReadOnlyList<Recipient>>(),
            Arg.Any<IReadOnlyList<Recipient>>(),
            Arg.Any<IReadOnlyList<Recipient>>(),
            Arg.Any<string>(),
            Arg.Any<string>());
        await _documentRepository.Received(1).MarkShareNotifiedAsync(9, Arg.Any<DateTime>());
    }

    [Fact]
    public async Task NotifySponsorsAsync_Omits_Additional_Address_That_Matches_The_Sponsor()
    {
        _documentRepository.GetUnnotifiedSharesAsync(42).Returns(
        [
            new DocumentShareNotificationDataModel
            {
                DocumentShareId = 1,
                RecipientEmail = "ana@ejemplo.com",
                RecipientName = "Ana",
                PublicAccessToken = Guid.NewGuid(),
                StudentId = 7,
                StudentFirstName = "María",
                StudentLastName = "García",
                StudentGender = Gender.Female,
                AdditionalRecipients =
                [
                    new SendAlsoToNotificationDataModel
                    {
                        RecipientName = "Ana Perez",
                        RecipientEmail = "  ANA@ejemplo.com ",
                        SendAsBcc = false,
                    },
                    new SendAlsoToNotificationDataModel
                    {
                        RecipientName = "Pedro Diaz",
                        RecipientEmail = "pedro@ejemplo.com",
                        SendAsBcc = true,
                    },
                ],
            },
        ]);

        await CreateService().NotifySponsorsAsync(42, TestContext.Current.CancellationToken);

        await _emailMessageSender.Received(1).SendEmailAsync(
            Arg.Is<IReadOnlyList<Recipient>>(to => Matches(to, new Recipient("ana@ejemplo.com"))),
            Arg.Is<IReadOnlyList<Recipient>>(cc => cc.Count == 0),
            Arg.Is<IReadOnlyList<Recipient>>(bcc => Matches(bcc, new Recipient("pedro@ejemplo.com", "Pedro Diaz"))),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task NotifySponsorsAsync_Sends_Duplicate_Additional_Address_Once_Preferring_Bcc()
    {
        _documentRepository.GetUnnotifiedSharesAsync(42).Returns(
        [
            new DocumentShareNotificationDataModel
            {
                DocumentShareId = 1,
                RecipientEmail = "ana@ejemplo.com",
                RecipientName = "Ana",
                PublicAccessToken = Guid.NewGuid(),
                StudentId = 7,
                StudentFirstName = "María",
                StudentLastName = "García",
                StudentGender = Gender.Female,
                AdditionalRecipients =
                [
                    new SendAlsoToNotificationDataModel
                    {
                        RecipientName = "Luis Perez",
                        RecipientEmail = "luis@ejemplo.com",
                        SendAsBcc = false,
                    },
                    new SendAlsoToNotificationDataModel
                    {
                        RecipientName = "Luis Perez",
                        RecipientEmail = " LUIS@ejemplo.com ",
                        SendAsBcc = true,
                    },
                ],
            },
        ]);

        await CreateService().NotifySponsorsAsync(42, TestContext.Current.CancellationToken);

        await _emailMessageSender.Received(1).SendEmailAsync(
            Arg.Any<IReadOnlyList<Recipient>>(),
            Arg.Is<IReadOnlyList<Recipient>>(cc => cc.Count == 0),
            Arg.Is<IReadOnlyList<Recipient>>(bcc => Matches(bcc, new Recipient("luis@ejemplo.com", "Luis Perez"))),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task NotifySponsorsAsync_Skips_Invalid_Additional_Address_And_Still_Sends()
    {
        _documentRepository.GetUnnotifiedSharesAsync(42).Returns(
        [
            new DocumentShareNotificationDataModel
            {
                DocumentShareId = 1,
                RecipientEmail = "ana@ejemplo.com",
                RecipientName = "Ana",
                PublicAccessToken = Guid.NewGuid(),
                StudentId = 7,
                StudentFirstName = "María",
                StudentLastName = "García",
                StudentGender = Gender.Female,
                AdditionalRecipients =
                [
                    new SendAlsoToNotificationDataModel
                    {
                        RecipientName = "Nope",
                        RecipientEmail = "not-an-email",
                        SendAsBcc = false,
                    },
                    new SendAlsoToNotificationDataModel
                    {
                        RecipientName = "Marta Gomez",
                        RecipientEmail = "marta@ejemplo.com",
                        SendAsBcc = true,
                    },
                ],
            },
        ]);

        await CreateService().NotifySponsorsAsync(42, TestContext.Current.CancellationToken);

        await _emailMessageSender.Received(1).SendEmailAsync(
            Arg.Is<IReadOnlyList<Recipient>>(to => Matches(to, new Recipient("ana@ejemplo.com"))),
            Arg.Is<IReadOnlyList<Recipient>>(cc => cc.Count == 0),
            Arg.Is<IReadOnlyList<Recipient>>(bcc => Matches(bcc, new Recipient("marta@ejemplo.com", "Marta Gomez"))),
            Arg.Any<string>(),
            Arg.Any<string>());
        await _documentRepository.Received(1).MarkShareNotifiedAsync(1, Arg.Any<DateTime>());
    }

    private static bool Matches(IReadOnlyList<Recipient> actual, params Recipient[] expected) =>
        actual.Count == expected.Length
        && actual.Zip(expected).All(pair =>
            pair.First.EmailAddress == pair.Second.EmailAddress
            && pair.First.DisplayName == pair.Second.DisplayName);
}