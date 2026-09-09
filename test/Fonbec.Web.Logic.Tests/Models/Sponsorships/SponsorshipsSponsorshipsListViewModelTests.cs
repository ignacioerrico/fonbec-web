using FluentAssertions;
using Fonbec.Web.Logic.Models.Sponsorships;

namespace Fonbec.Web.Logic.Tests.Models.Sponsorships;

public class SponsorshipsSponsorshipsListViewModelTests
{
    [Fact]
    public void TimelineStatus_IsNotStarted_When_StartDate_Is_In_The_Future()
    {
        var viewModel = new SponsorshipsSponsorshipsListViewModel
        {
            SponsorshipStartDate = DateTime.UtcNow.AddMonths(1),
            SponsorshipEndDate = DateTime.UtcNow.AddMonths(6),
        };

        viewModel.TimelineStatus.Should().Be(SponsorshipTimelineStatus.NotStarted);
        viewModel.IsCurrentlyActive.Should().BeFalse();
        viewModel.SponsorshipStatusLabel.Should().Be("No iniciado");
    }

    [Fact]
    public void TimelineStatus_IsActive_When_Now_Is_Within_The_Period()
    {
        var viewModel = new SponsorshipsSponsorshipsListViewModel
        {
            SponsorshipStartDate = DateTime.UtcNow.AddMonths(-1),
            SponsorshipEndDate = DateTime.UtcNow.AddMonths(1),
        };

        viewModel.TimelineStatus.Should().Be(SponsorshipTimelineStatus.Active);
        viewModel.IsCurrentlyActive.Should().BeTrue();
        viewModel.SponsorshipStatusLabel.Should().Be("Activo");
    }

    [Fact]
    public void TimelineStatus_IsActive_When_OpenEnded_And_Already_Started()
    {
        var viewModel = new SponsorshipsSponsorshipsListViewModel
        {
            SponsorshipStartDate = DateTime.UtcNow.AddMonths(-2),
            SponsorshipEndDate = null,
        };

        viewModel.TimelineStatus.Should().Be(SponsorshipTimelineStatus.Active);
        viewModel.IsCurrentlyActive.Should().BeTrue();
        viewModel.SponsorshipStatusLabel.Should().Be("Activo");
    }

    [Fact]
    public void TimelineStatus_IsFinished_When_EndDate_Is_In_The_Past()
    {
        var viewModel = new SponsorshipsSponsorshipsListViewModel
        {
            SponsorshipStartDate = DateTime.UtcNow.AddYears(-2),
            SponsorshipEndDate = DateTime.UtcNow.AddMonths(-1),
        };

        viewModel.TimelineStatus.Should().Be(SponsorshipTimelineStatus.Finished);
        viewModel.IsCurrentlyActive.Should().BeFalse();
        viewModel.SponsorshipStatusLabel.Should().Be("Finalizado");
    }
}
