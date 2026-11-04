using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Ui.Constants;

namespace Fonbec.Web.Ui.Tests.Navigation;

public class LandingRoutesTests
{
    [Theory]
    [InlineData(FonbecRole.Admin, false, false, NavRoutes.Chapters)]
    [InlineData(FonbecRole.Uploader, false, false, NavRoutes.FacilitatorStudents)]
    [InlineData(FonbecRole.Reviewer, true, true, NavRoutes.ImprovementQueue)]
    [InlineData(FonbecRole.Reviewer, true, false, NavRoutes.ReviewQueue)]
    [InlineData(FonbecRole.Reviewer, false, true, NavRoutes.ReviewQueue)]
    [InlineData(FonbecRole.Reviewer, false, false, NavRoutes.ReviewQueue)]
    [InlineData(FonbecRole.Manager, false, false, null)]
    [InlineData(FonbecRole.Manager, true, true, null)]
    public void For_SendsEachRoleToItsLandingPage(
        string role, bool canImproveImages, bool hasImagesToImprove, string? expected)
    {
        LandingRoutes.For(role, canImproveImages, hasImagesToImprove).Should().Be(expected);
    }
}