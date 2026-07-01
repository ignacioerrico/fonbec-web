using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Facilitators;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Logic.Tests.Models;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class FacilitatorServiceGetMisBecariosDashboardTests : MappingTestBase
{
    private readonly IFacilitatorRepository _facilitatorRepository;
    private readonly FacilitatorService _facilitatorService;

    public FacilitatorServiceGetMisBecariosDashboardTests()
    {
        _facilitatorRepository = Substitute.For<IFacilitatorRepository>();
        _facilitatorService = new FacilitatorService(_facilitatorRepository);
    }

    [Fact]
    public async Task GetMisBecariosDashboardAsync_Returns_Empty_Dashboard_When_No_Students()
    {
        _facilitatorRepository.GetMisBecariosDashboardAsync(2)
            .Returns(new MisBecariosDashboardDataModel { Students = [] });

        var result = await _facilitatorService.GetMisBecariosDashboardAsync(2);

        result.Students.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMisBecariosDashboardAsync_Maps_Repository_Results()
    {
        _facilitatorRepository.GetMisBecariosDashboardAsync(2)
            .Returns(new MisBecariosDashboardDataModel
            {
                Students =
                [
                    new MisBecariosRowDataModel
                    {
                        StudentId = 10,
                        StudentFirstName = "Ana",
                        StudentLastName = "Becaria",
                        StudentNickName = "Anita",
                        EducationLevel = EducationLevel.SecondarySchool,
                        Sponsors =
                        [
                            new DashboardSponsorDataModel
                            {
                                SponsorshipId = 30,
                                SponsorId = 20,
                                RecipientName = "Padrino Activo",
                                IsCompany = false,
                            },
                        ],
                    },
                ],
            });

        var result = await _facilitatorService.GetMisBecariosDashboardAsync(2);

        var student = result.Students.Should().ContainSingle().Which;
        student.StudentId.Should().Be(10);
        student.StudentFirstName.Should().Be("Ana");
        student.StudentNickName.Should().Be("Anita");
        student.EducationLevel.Should().Be(EducationLevel.SecondarySchool);

        student.Sponsors.Should().ContainSingle(s =>
            s.SponsorshipId == 30 && s.SponsorId == 20 && s.RecipientName == "Padrino Activo");
    }

    [Fact]
    public async Task GetMisBecariosDashboardAsync_Passes_FacilitatorId_To_Repository()
    {
        _facilitatorRepository.GetMisBecariosDashboardAsync(42)
            .Returns(new MisBecariosDashboardDataModel());

        await _facilitatorService.GetMisBecariosDashboardAsync(42);

        await _facilitatorRepository.Received(1).GetMisBecariosDashboardAsync(42);
    }
}
