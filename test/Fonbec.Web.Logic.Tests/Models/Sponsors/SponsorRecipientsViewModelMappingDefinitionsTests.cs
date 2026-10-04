using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Sponsors;
using Fonbec.Web.Logic.Models.Sponsors;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Sponsors;

public class SponsorRecipientsViewModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_Sponsor_Name_And_Recipients()
    {
        var input = new SponsorSendAlsoTosDataModel
        {
            SponsorId = 4,
            SponsorFirstName = "Ana",
            SponsorLastName = "Pérez",
            SponsorEmail = "ana@ejemplo.com",
            IsSponsorActive = true,
            Recipients =
            [
                new SendAlsoToDataModel
                {
                    Id = 9,
                    RecipientName = "Luis Pérez",
                    RecipientEmail = "luis@ejemplo.com",
                    SendAsBcc = true,
                },
            ],
        };

        var result = input.Adapt<SponsorRecipientsViewModel>(Config);

        result.SponsorId.Should().Be(4);
        result.SponsorFullName.Should().Be("Ana Pérez");
        result.SponsorEmail.Should().Be("ana@ejemplo.com");
        result.IsSponsorActive.Should().BeTrue();
        var recipient = result.Recipients.Should().ContainSingle().Subject;
        recipient.Id.Should().Be(9);
        recipient.RecipientName.Should().Be("Luis Pérez");
        recipient.RecipientEmail.Should().Be("luis@ejemplo.com");
        recipient.SendAsBcc.Should().BeTrue();
    }
}