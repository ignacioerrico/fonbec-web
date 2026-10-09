using System.Net;
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
            "mediador@test.com",
            "Nuevo mensaje para Camila Escobar",
            Arg.Is<string>(html =>
                html.Contains($"para la becaria <strong>{WebUtility.HtmlEncode("Camila Escobar")}</strong> de su madrina <strong>{WebUtility.HtmlEncode("Claudia Romero")}</strong>:")
                && html.Contains("Hacele llegar el mensaje a <strong>Cami</strong> y <strong>marcalo como compartido</strong>")
                && html.Contains("CUERPO-SECRETO<br>hola &lt;b&gt;becario&lt;/b&gt;")
                && html.Contains("https://fonbec.test/mensajes-para-becarios")
                && html.Contains("Ver todos los mensajes para becarios")
                && !html.Contains("/padrinos")
                && !html.Contains("/empresas")
                && !html.Contains("<b>becario</b>")));
        await _emailMessageSender.DidNotReceive().SendEmailAsync(
            Arg.Is<string>(email => email != "mediador@test.com"),
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
            "mediador@test.com",
            "Nuevo mensaje para María López",
            Arg.Is<string>(html =>
                html.Contains($"para la becaria <strong>{WebUtility.HtmlEncode("María López")}</strong> de la empresa <strong>Acme SA</strong>:")
                && html.Contains($"Hacele llegar el mensaje a <strong>{WebUtility.HtmlEncode("María")}</strong>")
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
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await _repository.DidNotReceive().MarkFacilitatorNotifiedAsync(Arg.Any<long>(), Arg.Any<DateTime>());
        _logger.Text.Should().NotContain("CUERPO-SECRETO");
        _logger.Text.Should().Contain(MessageId.ToString());
    }

    [Fact]
    public async Task Notify_WhenEveryAttemptFails_DoesNotThrowAndLeavesUnnotified()
    {
        Notice(isCompany: false, sender: "Ana Pérez", student: "Juan García");
        _emailMessageSender
            .SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .ThrowsAsync(new InvalidOperationException("permanent"));

        var act = async () => await CreateService().NotifyFacilitatorOfRecipientMessageAsync(
            MessageId, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
        await _emailMessageSender.Received(3).SendEmailAsync(
            "mediador@test.com", Arg.Any<string>(), Arg.Any<string>());
        await _repository.DidNotReceive().MarkFacilitatorNotifiedAsync(Arg.Any<long>(), Arg.Any<DateTime>());
        _logger.Text.Should().NotContain("CUERPO-SECRETO");
    }

    [Fact]
    public async Task Notify_RetriesThenMarksNotified()
    {
        Notice(isCompany: false, sender: "Ana Pérez", student: "Juan García");
        _emailMessageSender
            .SendEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(
                _ => throw new InvalidOperationException("transient"),
                _ => Task.CompletedTask);

        await CreateService().NotifyFacilitatorOfRecipientMessageAsync(MessageId, TestContext.Current.CancellationToken);

        await _emailMessageSender.Received(2).SendEmailAsync(
            "mediador@test.com", Arg.Any<string>(), Arg.Any<string>());
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
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await _repository.DidNotReceive().MarkFacilitatorNotifiedAsync(Arg.Any<long>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Notify_MissingBaseUrl_DoesNotSend()
    {
        Notice(isCompany: false, sender: "Ana Pérez", student: "Juan García");
        _configuration["App:BaseUrl"].Returns("  ");

        await CreateService().NotifyFacilitatorOfRecipientMessageAsync(MessageId, TestContext.Current.CancellationToken);

        await _emailMessageSender.DidNotReceive().SendEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
        await _repository.DidNotReceive().MarkFacilitatorNotifiedAsync(Arg.Any<long>(), Arg.Any<DateTime>());
    }

    [Fact]
    public void Html_EncodesNamesAndKeepsLineBreaks_OnTheStaffPageLink()
    {
        var html = RecipientMessageNotificationFormatter.BuildHtml(
            "Juan <García>",
            Gender.Male,
            studentFirstName: "Juan",
            studentNickName: "  ",
            isCompany: false,
            senderGender: Gender.Female,
            senderName: "Ana & Pérez",
            body: "línea 1\r\nlínea 2",
            messagesUrl: "https://fonbec.test/mensajes-para-becarios");

        var encodedLines = WebUtility.HtmlEncode("línea 1\r\nlínea 2")
            .Replace("\r\n", "<br>", StringComparison.Ordinal);
        html.Should().Contain($"para el becario <strong>{WebUtility.HtmlEncode("Juan <García>")}</strong> de su madrina <strong>{WebUtility.HtmlEncode("Ana & Pérez")}</strong>:");
        html.Should().Contain("Hacele llegar el mensaje a <strong>Juan</strong>");
        html.Should().Contain($"<blockquote>{encodedLines}</blockquote>");
        html.Should().NotContain("Juan <García>");
        html.Should().NotContain("Ana & Pérez");
        html.Should().Contain(">Ver todos los mensajes para becarios</a>");
        html.Should().Contain("href=\"https://fonbec.test/mensajes-para-becarios\"");
        html.Should().NotContain("/padrinos");
        html.Should().NotContain("/empresas");
        RecipientMessageNotificationFormatter.BuildSubject("Juan\nGarcía")
            .Should().Be("Nuevo mensaje para Juan García");
    }

    [Fact]
    public void Html_UsesPadrinoAndBecario_AndFallsBackWhenGenderIsUnknown()
    {
        var male = RecipientMessageNotificationFormatter.BuildHtml(
            "Juan García",
            Gender.Male,
            "Juan",
            "Juancito",
            isCompany: false,
            senderGender: Gender.Male,
            senderName: "Luis Gómez",
            body: "hola",
            messagesUrl: "https://fonbec.test/mensajes-para-becarios");
        male.Should().Contain($"para el becario <strong>{WebUtility.HtmlEncode("Juan García")}</strong> de su padrino <strong>{WebUtility.HtmlEncode("Luis Gómez")}</strong>:");
        male.Should().Contain("a <strong>Juancito</strong>");

        var unknown = RecipientMessageNotificationFormatter.BuildHtml(
            "Alex García",
            Gender.Unknown,
            "Alex",
            null,
            isCompany: false,
            senderGender: null,
            senderName: "Río",
            body: "hola",
            messagesUrl: "https://fonbec.test/mensajes-para-becarios");
        unknown.Should().Contain($"para el becario/la becaria <strong>{WebUtility.HtmlEncode("Alex García")}</strong> de su padrino/su madrina <strong>{WebUtility.HtmlEncode("Río")}</strong>:");
        unknown.Should().Contain("a <strong>Alex</strong>");
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
                FacilitatorNotifiedOn = notifiedOn,
                StudentFullName = student,
                StudentFirstName = studentFirstName,
                StudentNickName = studentNickName,
                StudentGender = studentGender,
                IsCompany = isCompany,
                SenderGender = isCompany ? null : senderGender,
                SenderName = sender,
                Body = Body,
            });
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