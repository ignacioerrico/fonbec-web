using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.RecipientMessages;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Logic.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Fonbec.Web.Logic.Tests.Services;

public class RecipientMessageNotificationServiceTests
{
    private const long MessageId = 15;
    private const string Body = "CUERPO-SECRETO\nhola <b>becario</b>";
    private static readonly DateTimeOffset UtcNow = new(2026, 10, 8, 18, 30, 0, TimeSpan.Zero);

    private readonly IRecipientMessageRepository _repository = Substitute.For<IRecipientMessageRepository>();
    private readonly IEmailMessageSender _emailMessageSender = Substitute.For<IEmailMessageSender>();
    private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
    private readonly ListLogger _logger = new();

    public RecipientMessageNotificationServiceTests()
    {
        _configuration["App:BaseUrl"].Returns("https://fonbec.test/");
    }

    [Fact]
    public async Task Notify_PersonSponsor_EmailsOnlyTheCurrentFacilitator()
    {
        Notice(
            isCompany: false,
            sender: "Claudia Romero",
            student: "Camila Escobar",
            studentFirstName: "Camila",
            studentNickName: "Cami",
            studentGender: Gender.Female,
            senderGender: Gender.Female);

        await CreateService().NotifyFacilitatorOfRecipientMessageAsync(MessageId, TestContext.Current.CancellationToken);

        await _emailMessageSender.Received(1).SendEmailAsync(
            Arg.Is<Recipient>(recipient =>
                recipient.EmailAddress == "<mediador@test.com>"
                && recipient.DisplayName == "\"Mediador Test\""),
            "FONBEC | Nuevo mensaje para Cami Escobar",
            Arg.Is<string>(html =>
                html.Contains("para la becaria Cami Escobar de su madrina Claudia:")
                && html.Contains("Hacele llegar el mensaje a Cami y marcalo como compartido")
                && html.Contains("CUERPO-SECRETO<br>hola &lt;b&gt;becario&lt;/b&gt;")
                && html.Contains("https://fonbec.test/mensajes-para-becarios")
                && html.Contains("Ver todos los mensajes para becarios")
                && !html.Contains("/padrinos")
                && !html.Contains("/empresas")
                && !html.Contains("<b>becario</b>")));
        await _emailMessageSender.DidNotReceive().SendEmailAsync(
            Arg.Is<Recipient>(recipient => recipient.EmailAddress != "<mediador@test.com>"),
            Arg.Any<string>(),
            Arg.Any<string>());
        await _emailMessageSender.DidNotReceive().SendEmailAsync(
            Arg.Any<IReadOnlyList<Recipient>>(),
            Arg.Any<IReadOnlyList<Recipient>>(),
            Arg.Any<IReadOnlyList<Recipient>>(),
            Arg.Any<string>(),
            Arg.Any<string>());
        await _repository.Received(1).MarkFacilitatorNotifiedAsync(MessageId, UtcNow.UtcDateTime);
        _logger.Text.Should().NotContain(Body);
    }

    [Fact]
    public async Task Notify_Company_NamesTheEmpresaAndStillEmailsOnlyTheFacilitator()
    {
        Notice(
            isCompany: true,
            sender: "Acme SA",
            student: "María López",
            studentFirstName: "María",
            studentGender: Gender.Female);

        await CreateService().NotifyFacilitatorOfRecipientMessageAsync(MessageId, TestContext.Current.CancellationToken);

        await _emailMessageSender.Received(1).SendEmailAsync(
            Arg.Is<Recipient>(recipient =>
                recipient.EmailAddress == "<mediador@test.com>"
                && recipient.DisplayName == "\"Mediador Test\""),
            "FONBEC | Nuevo mensaje para María López",
            Arg.Is<string>(html =>
                html.Contains("para la becaria María López de la empresa Acme SA:")
                && html.Contains("Hacele llegar el mensaje a María")
                && !html.Contains("su padrino")
                && !html.Contains("su madrina")
                && html.Contains("https://fonbec.test/mensajes-para-becarios")));
        await _repository.Received(1).MarkFacilitatorNotifiedAsync(MessageId, UtcNow.UtcDateTime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Notify_BlankFacilitatorEmail_DoesNotSendAndLeavesUnnotified(string? email)
    {
        Notice(isCompany: false, sender: "Ana Pérez", student: "Juan García", email: email);

        await CreateService().NotifyFacilitatorOfRecipientMessageAsync(MessageId, TestContext.Current.CancellationToken);

        await _emailMessageSender.DidNotReceive().SendEmailAsync(
            Arg.Any<Recipient>(), Arg.Any<string>(), Arg.Any<string>());
        await _repository.DidNotReceive().MarkFacilitatorNotifiedAsync(Arg.Any<long>(), Arg.Any<DateTime>());
        _logger.Text.Should().NotContain("CUERPO-SECRETO");
        _logger.Text.Should().Contain(MessageId.ToString());
    }

    [Fact]
    public async Task Notify_WhenEveryAttemptFails_DoesNotThrowAndLeavesUnnotified()
    {
        Notice(isCompany: false, sender: "Ana Pérez", student: "Juan García");
        _emailMessageSender
            .SendEmailAsync(Arg.Any<Recipient>(), Arg.Any<string>(), Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException("permanent"));

        var act = async () => await CreateService().NotifyFacilitatorOfRecipientMessageAsync(
            MessageId, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
        await _emailMessageSender.Received(3).SendEmailAsync(
            Arg.Is<Recipient>(recipient => recipient.EmailAddress == "<mediador@test.com>"),
            Arg.Any<string>(),
            Arg.Any<string>());
        await _repository.DidNotReceive().MarkFacilitatorNotifiedAsync(Arg.Any<long>(), Arg.Any<DateTime>());
        _logger.Text.Should().NotContain("CUERPO-SECRETO");
    }

    [Fact]
    public async Task Notify_RetriesThenMarksNotified()
    {
        Notice(isCompany: false, sender: "Ana Pérez", student: "Juan García");
        _emailMessageSender
            .SendEmailAsync(Arg.Any<Recipient>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(
                _ => throw new InvalidOperationException("transient"),
                _ => Task.CompletedTask);

        await CreateService().NotifyFacilitatorOfRecipientMessageAsync(MessageId, TestContext.Current.CancellationToken);

        await _emailMessageSender.Received(2).SendEmailAsync(
            Arg.Is<Recipient>(recipient => recipient.EmailAddress == "<mediador@test.com>"),
            Arg.Any<string>(),
            Arg.Any<string>());
        await _repository.Received(1).MarkFacilitatorNotifiedAsync(MessageId, UtcNow.UtcDateTime);
    }

    [Fact]
    public async Task Notify_AlreadyNotified_DoesNotSendAgain()
    {
        Notice(
            isCompany: false,
            sender: "Ana Pérez",
            student: "Juan García",
            notifiedOn: UtcNow.UtcDateTime);

        await CreateService().NotifyFacilitatorOfRecipientMessageAsync(MessageId, TestContext.Current.CancellationToken);

        await _emailMessageSender.DidNotReceive().SendEmailAsync(
            Arg.Any<Recipient>(), Arg.Any<string>(), Arg.Any<string>());
        await _repository.DidNotReceive().MarkFacilitatorNotifiedAsync(Arg.Any<long>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Notify_MissingBaseUrl_DoesNotSend()
    {
        Notice(isCompany: false, sender: "Ana Pérez", student: "Juan García");
        _configuration["App:BaseUrl"].Returns("  ");

        await CreateService().NotifyFacilitatorOfRecipientMessageAsync(MessageId, TestContext.Current.CancellationToken);

        await _emailMessageSender.DidNotReceive().SendEmailAsync(
            Arg.Any<Recipient>(), Arg.Any<string>(), Arg.Any<string>());
        await _repository.DidNotReceive().MarkFacilitatorNotifiedAsync(Arg.Any<long>(), Arg.Any<DateTime>());
    }

    private RecipientMessageNotificationService CreateService() =>
        new(_repository, _emailMessageSender, _configuration, new FixedTimeProvider(UtcNow), _logger);

    private void Notice(
        bool isCompany,
        string sender,
        string student,
        string? email = "mediador@test.com",
        DateTime? notifiedOn = null,
        string studentFirstName = "Juan",
        string? studentNickName = null,
        Gender studentGender = Gender.Male,
        Gender? senderGender = Gender.Male)
    {
        _repository.GetFacilitatorNotificationAsync(MessageId).Returns(
            new RecipientMessageFacilitatorNotificationDataModel
            {
                FacilitatorEmail = email,
                FacilitatorFullName = "Mediador Test",
                FacilitatorNotifiedOn = notifiedOn,
                StudentFullName = student,
                StudentFirstName = studentFirstName,
                StudentLastName = LastWord(student),
                StudentNickName = studentNickName,
                StudentGender = studentGender,
                FacilitatorFirstName = "Mediador",
                IsCompany = isCompany,
                SenderGender = isCompany ? null : senderGender,
                SenderName = sender,
                SenderFirstName = isCompany ? sender : FirstWord(sender),
                SenderLastName = isCompany ? string.Empty : LastWord(sender),
                Body = Body,
            });
    }

    private static string FirstWord(string value)
    {
        var space = value.IndexOf(' ');
        return space < 0 ? value : value[..space];
    }

    private static string LastWord(string value)
    {
        var space = value.LastIndexOf(' ');
        return space < 0 ? string.Empty : value[(space + 1)..];
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class ListLogger : ILogger<RecipientMessageNotificationService>
    {
        public string Text { get; private set; } = "";

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Text += formatter(state, exception);
            if (exception is not null)
            {
                Text += exception.Message;
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}