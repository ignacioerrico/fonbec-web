using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Sponsorships;
using Fonbec.Web.DataAccess.DataModels.Sponsorships.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Models.Sponsorships;
using Fonbec.Web.Logic.Models.Sponsorships.Input;
using Fonbec.Web.Logic.Services;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class SponsorshipServiceTests
{
    private readonly ISponsorshipRepository _repository = Substitute.For<ISponsorshipRepository>();
    private readonly SponsorshipService _service;

    public SponsorshipServiceTests()
    {
        _service = new SponsorshipService(_repository);
    }

    [Theory]
    [InlineData(SponsorshipPeriodMatch.None, SponsorshipPeriodStatus.Available)]
    [InlineData(SponsorshipPeriodMatch.Overlap, SponsorshipPeriodStatus.OverlapsExisting)]
    [InlineData(SponsorshipPeriodMatch.Adjacent, SponsorshipPeriodStatus.ExtendsExisting)]
    public async Task GetSponsorshipPeriodStatusAsync_Maps_Repository_Match(
        SponsorshipPeriodMatch match,
        SponsorshipPeriodStatus expected)
    {
        _repository.GetCreateSponsorshipPreviewAsync(Arg.Any<CreateSponsorshipInputDataModel>())
            .Returns(new CreateSponsorshipPreviewDataModel { PeriodMatch = match });

        var result = await _service.GetSponsorshipPeriodStatusAsync(CreateInput());

        result.Should().Be(expected);
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Reports_Extension()
    {
        _repository.CreateSponsorshipAsync(Arg.Any<CreateSponsorshipInputDataModel>())
            .Returns(new CreateSponsorshipRepositoryResult(1, SponsorshipPeriodMatch.Adjacent));

        var result = await _service.CreateSponsorshipAsync(CreateInput());

        result.AnyAffectedRows.Should().BeTrue();
        result.PeriodStatus.Should().Be(SponsorshipPeriodStatus.ExtendsExisting);
    }

    [Fact]
    public async Task UpdateSponsorshipAsync_Maps_Locked_Plan_Months()
    {
        _repository.UpdateSponsorshipAsync(Arg.Any<UpdateSponsorshipInputDataModel>())
            .Returns(new UpdateSponsorshipRepositoryResult(
                0,
                UpdateSponsorshipOutcome.UncoversLockedPlan,
                [new DateTime(2026, 9, 1)]));

        var result = await _service.UpdateSponsorshipAsync(
            new UpdateSponsorshipInputModel(
                SponsorshipId: 1,
                SponsorshipStartDate: new DateTime(2026, 1, 1),
                SponsorshipEndDate: new DateTime(2026, 8, 31),
                SponsorshipNotes: string.Empty,
                UpdatedById: 30));

        result.Status.Should().Be(UpdateSponsorshipStatus.UncoversLockedPlan);
        result.LockedPlanMonthLabels.Should().Equal("Septiembre de 2026");
        result.AnyAffectedRows.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateSponsorshipAsync_Maps_Overlap()
    {
        _repository.UpdateSponsorshipAsync(Arg.Any<UpdateSponsorshipInputDataModel>())
            .Returns(new UpdateSponsorshipRepositoryResult(
                Outcome: UpdateSponsorshipOutcome.Overlap,
                PeriodMatch: SponsorshipPeriodMatch.Overlap));

        var result = await _service.UpdateSponsorshipAsync(
            new UpdateSponsorshipInputModel(
                SponsorshipId: 1,
                SponsorshipStartDate: new DateTime(2026, 1, 1),
                SponsorshipEndDate: new DateTime(2026, 6, 30),
                SponsorshipNotes: string.Empty,
                UpdatedById: 30));

        result.Status.Should().Be(UpdateSponsorshipStatus.OverlapsExisting);
    }

    [Fact]
    public async Task UpdateSponsorshipAsync_Maps_Exemptions_Requiring_Confirmation()
    {
        _repository.UpdateSponsorshipAsync(Arg.Any<UpdateSponsorshipInputDataModel>())
            .Returns(new UpdateSponsorshipRepositoryResult(
                Outcome: UpdateSponsorshipOutcome.RequiresExemptionRevocation,
                ExemptPlanStartsOn: [new DateTime(2026, 9, 1)]));

        var result = await _service.UpdateSponsorshipAsync(
            new UpdateSponsorshipInputModel(
                SponsorshipId: 1,
                SponsorshipStartDate: new DateTime(2026, 1, 1),
                SponsorshipEndDate: new DateTime(2026, 8, 31),
                SponsorshipNotes: string.Empty,
                UpdatedById: 30));

        result.Status.Should().Be(UpdateSponsorshipStatus.RequiresExemptionRevocation);
        result.ExemptPlanMonthLabels.Should().Equal("Septiembre de 2026");
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Maps_Completed_Plan_Months()
    {
        _repository.CreateSponsorshipAsync(Arg.Any<CreateSponsorshipInputDataModel>())
            .Returns(new CreateSponsorshipRepositoryResult(
                CompletedPlanStartsOn: [new DateTime(2026, 9, 1)]));

        var result = await _service.CreateSponsorshipAsync(CreateInput());

        result.AnyAffectedRows.Should().BeFalse();
        result.CompletedPlanMonthLabels.Should().Equal("Septiembre de 2026");
    }

    [Fact]
    public async Task GetCreateSponsorshipPreviewAsync_Maps_Overlapping_Sponsorships_To_End()
    {
        _repository.GetCreateSponsorshipPreviewAsync(Arg.Any<CreateSponsorshipInputDataModel>())
            .Returns(new CreateSponsorshipPreviewDataModel
            {
                PeriodMatch = SponsorshipPeriodMatch.None,
                OverlappingToEnd =
                [
                    new OverlappingSponsorshipToEndDataModel
                    {
                        SponsorshipId = 7,
                        RecipientName = "Carlos Padrino",
                        StartDate = new DateTime(2026, 3, 1),
                        ProposedEndDate = new DateTime(2026, 8, 31),
                    },
                ],
            });

        var result = await _service.GetCreateSponsorshipPreviewAsync(CreateInput());

        result.PeriodStatus.Should().Be(SponsorshipPeriodStatus.Available);
        var item = result.OverlappingToEnd.Should().ContainSingle().Which;
        item.RecipientName.Should().Be("Carlos Padrino");
        item.StartMonthLabel.Should().Be("Marzo de 2026");
        item.ProposedEndMonthLabel.Should().Be("Agosto de 2026");
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Maps_Locked_Plan_Months_When_Ending_Overlapping()
    {
        _repository.CreateSponsorshipAsync(Arg.Any<CreateSponsorshipInputDataModel>())
            .Returns(new CreateSponsorshipRepositoryResult(
                UncoveredPlanStartsOn: [new DateTime(2026, 9, 1)]));

        var result = await _service.CreateSponsorshipAsync(CreateInput());

        result.AnyAffectedRows.Should().BeFalse();
        result.LockedPlanMonthLabels.Should().Equal("Septiembre de 2026");
    }

    [Fact]
    public async Task UpdateSponsorshipAsync_Maps_Completed_Plan_Months()
    {
        _repository.UpdateSponsorshipAsync(Arg.Any<UpdateSponsorshipInputDataModel>())
            .Returns(new UpdateSponsorshipRepositoryResult(
                Outcome: UpdateSponsorshipOutcome.AddsSlotToCompletedPlan,
                CompletedPlanStartsOn: [new DateTime(2026, 9, 1)]));

        var result = await _service.UpdateSponsorshipAsync(
            new UpdateSponsorshipInputModel(
                SponsorshipId: 1,
                SponsorshipStartDate: new DateTime(2026, 1, 1),
                SponsorshipEndDate: new DateTime(2026, 9, 30),
                SponsorshipNotes: string.Empty,
                UpdatedById: 30));

        result.Status.Should().Be(UpdateSponsorshipStatus.AddsSlotToCompletedPlan);
        result.CompletedPlanMonthLabels.Should().Equal("Septiembre de 2026");
    }

    private static CreateSponsorshipInputModel CreateInput() =>
        new(
            StudentId: 10,
            Sponsor: new SelectableModel<int>(20, "Padrino"),
            CompanyId: null,
            SponsorshipStartDate: new DateTime(2026, 1, 1),
            SponsorshipEndDate: new DateTime(2026, 6, 30),
            SponsorshipNotes: string.Empty,
            CreatedById: 30);
}