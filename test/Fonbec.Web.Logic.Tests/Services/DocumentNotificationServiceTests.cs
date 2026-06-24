using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Logic.Util;
using Microsoft.Extensions.Configuration;
using NSubstitute;

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

    [Fact]
    public async Task NotifySponsorsAsync_Sends_Email_With_Personalized_Content()
    {
        var token = Guid.NewGuid();
        _documentRepository.GetUnnotifiedSharesAsync(42).Returns(
        [
            new DocumentShareNotificationDataModel
            {
                DocumentShareId = 1,
                SponsorEmail = "padrino@test.com",
                SponsorFirstName = "Juan",
                SponsorNickName = "Juancito",
                PublicAccessToken = token,
                StudentId = 7,
                StudentFirstName = "María",
                StudentLastName = "García",
                StudentNickName = "Mari",
                StudentGender = Gender.Female,
            },
        ]);

        var service = new DocumentNotificationService(
            _documentRepository, _emailMessageSender, _configuration);

        await service.NotifySponsorsAsync(42);

        await _emailMessageSender.Received(1).SendEmailAsync(
            "padrino@test.com",
            "Nuevo documento disponible",
            Arg.Is<string>(html =>
                html.Contains("Hola, Juancito:")
                && html.Contains("de tu ahijada Mari García.")
                && html.Contains($"https://fonbec.test/padrinos/{token}/7")));

        await _documentRepository.Received(1).MarkShareNotifiedAsync(1, Arg.Any<DateTime>());
    }
}