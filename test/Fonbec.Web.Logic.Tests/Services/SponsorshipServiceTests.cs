using FluentAssertions;
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
        _repository.GetSponsorshipPeriodMatchAsync(Arg.Any<CreateSponsorshipInputDataModel>())
            .Returns(match);

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