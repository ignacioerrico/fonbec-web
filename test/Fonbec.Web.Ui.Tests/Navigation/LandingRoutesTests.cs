using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Ui.Constants;

namespace Fonbec.Web.Ui.Tests.Navigation;

public class LandingRoutesTests
{
    [Theory]
    [InlineData(FonbecRole.Admin, false, NavRoutes.Chapters)]
    [InlineData(FonbecRole.Uploader, false, NavRoutes.FacilitatorStudents)]
    [InlineData(FonbecRole.Reviewer, true, NavRoutes.ImprovementQueue)]
    [InlineData(FonbecRole.Reviewer, false, NavRoutes.ReviewQueue)]
    [InlineData(FonbecRole.Manager, false, null)]
    [InlineData(FonbecRole.Manager, true, null)]
    public void For_SendsEachRoleToItsLandingPage(string role, bool canImproveImages, string? expected)
    {
        LandingRoutes.For(role, canImproveImages).Should().Be(expected);
    }
}