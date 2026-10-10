using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Companies;
using Fonbec.Web.Logic.Models.Companies;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Companies;

public class CompanyRelationsViewModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_Contacts_And_Sponsors()
    {
        var dataModel = new CompanyRelationsDataModel
        {
            CompanyId = 7,
            CompanyName = "Acme",
            Contacts =
            [
                new CompanyContactDataModel
                {
                    Id = 4,
                    FirstName = "Ana",
                    LastName = null,
                    Email = null,
                },
            ],
            Sponsors =
            [
                new CompanyLinkedSponsorDataModel { Id = 9, FullName = "Luis Pérez" },
            ],
        };

        var viewModel = dataModel.Adapt<CompanyRelationsViewModel>(Config);

        viewModel.CompanyId.Should().Be(7);
        viewModel.CompanyName.Should().Be("Acme");
        viewModel.Contacts.Should().ContainSingle(contact =>
            contact.Id == 4 && contact.FirstName == "Ana" && contact.LastName == string.Empty && contact.Email == string.Empty);
        viewModel.Sponsors.Should().ContainSingle(sponsor => sponsor.Key == 9 && sponsor.DisplayName == "Luis Pérez");
    }
}