using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Students;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Logic.Tests.Models;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class FacilitatorServiceLetterStatusTests : MappingTestBase
{
    private const int FacilitatorId = 2;
    private const int StudentId = 10;
    private const int PlanId = 7;
    private const int SponsorId = 20;
    private const int CompanyId = 40;

    private readonly IFacilitatorRepository _facilitatorRepository;
    private readonly ILetterExemptionService _letterExemptionService;
    private readonly FacilitatorService _facilitatorService;

    public FacilitatorServiceLetterStatusTests()
    {
        _facilitatorRepository = Substitute.For<IFacilitatorRepository>();
        _letterExemptionService = Substitute.For<ILetterExemptionService>();
        _letterExemptionService.GetActiveExemptionReasonsForPlanAsync(Arg.Any<int>()).Returns(new Dictionary<int, string>());
        _facilitatorService = new FacilitatorService(_facilitatorRepository, _letterExemptionService);

        _facilitatorRepository.GetCurrentPlanForFacilitatorAsync(FacilitatorId)
            .Returns(new CurrentPlanDataModel { PlanId = PlanId, StartsOn = new DateTime(2026, 6, 1) });
    }

    [Fact]
    public async Task GetStudentsDashboardAsync_Sets_NoPlan_When_No_Current_Plan()
    {
        _facilitatorRepository.GetCurrentPlanForFacilitatorAsync(FacilitatorId).Returns((CurrentPlanDataModel?)null);
        SetupStudentWithOneSponsor();

        var result = await _facilitatorService.GetStudentsDashboardAsync(FacilitatorId);

        result.Students.Single().LetterAggregate.Should().Be(LetterAggregateStatus.NoPlan);
        await _facilitatorRepository.DidNotReceive().GetCurrentLetterStatusesAsync(Arg.Any<int>(), Arg.Any<List<int>>());
    }

    [Fact]
    public async Task GetStudentsDashboardAsync_Sets_Complete_When_All_Sponsors_Have_A_Letter()
    {
        SetupStudentWithOneSponsor();
        _facilitatorRepository.GetCurrentLetterStatusesAsync(PlanId, Arg.Any<List<int>>())
            .Returns([
                new SponsorLetterStatusDataModel { StudentId = StudentId, SponsorId = SponsorId, Status = DocumentStatus.Approved },
            ]);

        var result = await _facilitatorService.GetStudentsDashboardAsync(FacilitatorId);

        result.Students.Single().LetterAggregate.Should().Be(LetterAggregateStatus.Complete);
    }

    [Fact]
    public async Task GetStudentsDashboardAsync_Sets_NotUploaded_When_Sponsor_Has_No_Letter()
    {
        SetupStudentWithOneSponsor();
        _facilitatorRepository.GetCurrentLetterStatusesAsync(PlanId, Arg.Any<List<int>>())
            .Returns([]);

        var result = await _facilitatorService.GetStudentsDashboardAsync(FacilitatorId);

        result.Students.Single().LetterAggregate.Should().Be(LetterAggregateStatus.NotUploaded);
    }

    [Fact]
    public async Task GetStudentsDashboardAsync_Sets_Partial_When_One_Uploaded_And_One_Rejected()
    {
        SetupStudentWithTwoSponsors();
        _facilitatorRepository.GetCurrentLetterStatusesAsync(PlanId, Arg.Any<List<int>>())
            .Returns([
                new SponsorLetterStatusDataModel { StudentId = StudentId, SponsorId = SponsorId, Status = DocumentStatus.Approved },
                new SponsorLetterStatusDataModel { StudentId = StudentId, CompanyId = CompanyId, Status = DocumentStatus.Rejected },
            ]);

        var result = await _facilitatorService.GetStudentsDashboardAsync(FacilitatorId);

        var student = result.Students.Single();
        student.LetterAggregate.Should().Be(LetterAggregateStatus.Partial);
        student.UploadedLetterCount.Should().Be(1);
        student.SponsorLetterCount.Should().Be(2);
        student.SponsorsNeedingLetter.Should().ContainSingle(s => s.CompanyId == CompanyId);
    }

    [Fact]
    public async Task GetStudentsDashboardAsync_Sets_Exempt_When_Student_Is_Exempt_For_Current_Plan()
    {
        SetupStudentWithOneSponsor();
        _facilitatorRepository.GetCurrentLetterStatusesAsync(PlanId, Arg.Any<List<int>>()).Returns([]);
        _letterExemptionService.GetActiveExemptionReasonsForPlanAsync(PlanId)
            .Returns(new Dictionary<int, string> { [StudentId] = "Beca de intercambio" });

        var result = await _facilitatorService.GetStudentsDashboardAsync(FacilitatorId);

        var student = result.Students.Single();
        student.LetterAggregate.Should().Be(LetterAggregateStatus.Exempt);
        student.LetterExemptionReason.Should().Be("Beca de intercambio");
    }

    [Fact]
    public async Task GetStudentsDashboardAsync_Maps_Rejection_Reason_For_Rejected_Sponsor()
    {
        SetupStudentWithOneSponsor();
        _facilitatorRepository.GetCurrentLetterStatusesAsync(PlanId, Arg.Any<List<int>>())
            .Returns([
                new SponsorLetterStatusDataModel
                {
                    StudentId = StudentId,
                    SponsorId = SponsorId,
                    Status = DocumentStatus.Rejected,
                    RejectionReason = "foto borrosa",
                },
            ]);

        var result = await _facilitatorService.GetStudentsDashboardAsync(FacilitatorId);

        var sponsorStatus = result.Students.Single().LetterStatuses.Single();
        sponsorStatus.Status.Should().Be(LetterSlotStatus.Rejected);
        sponsorStatus.RejectionReason.Should().Be("foto borrosa");
    }

    [Fact]
    public async Task GetStudentsDashboardAsync_Does_Not_Set_Rejection_Reason_When_Not_Rejected()
    {
        SetupStudentWithOneSponsor();
        _facilitatorRepository.GetCurrentLetterStatusesAsync(PlanId, Arg.Any<List<int>>())
            .Returns([
                new SponsorLetterStatusDataModel
                {
                    StudentId = StudentId,
                    SponsorId = SponsorId,
                    Status = DocumentStatus.Approved,
                    RejectionReason = "stale reason that should never be surfaced",
                },
            ]);

        var result = await _facilitatorService.GetStudentsDashboardAsync(FacilitatorId);

        result.Students.Single().LetterStatuses.Single().RejectionReason.Should().BeNull();
    }

    [Fact]
    public async Task GetStudentsDashboardAsync_Maps_Sin_Carta_When_No_Letter_Uploaded_Yet()
    {
        SetupStudentWithOneSponsor();
        _facilitatorRepository.GetCurrentLetterStatusesAsync(PlanId, Arg.Any<List<int>>())
            .Returns([]);

        var result = await _facilitatorService.GetStudentsDashboardAsync(FacilitatorId);

        result.Students.Single().LetterStatuses.Single().Status.Should().Be(LetterSlotStatus.None);
    }

    [Fact]
    public async Task GetStudentsDashboardAsync_Maps_En_Revision_When_Letter_Uploaded_But_Not_Yet_Reviewed()
    {
        SetupStudentWithOneSponsor();
        _facilitatorRepository.GetCurrentLetterStatusesAsync(PlanId, Arg.Any<List<int>>())
            .Returns([
                new SponsorLetterStatusDataModel { StudentId = StudentId, SponsorId = SponsorId, Status = DocumentStatus.Processing },
            ]);

        var result = await _facilitatorService.GetStudentsDashboardAsync(FacilitatorId);

        result.Students.Single().LetterStatuses.Single().Status.Should().Be(LetterSlotStatus.InReview);
    }

    [Fact]
    public async Task GetStudentsDashboardAsync_Maps_Sponsor_Identity_Onto_LetterStatuses()
    {
        SetupStudentWithOneSponsor();
        _facilitatorRepository.GetCurrentLetterStatusesAsync(PlanId, Arg.Any<List<int>>()).Returns([]);

        var result = await _facilitatorService.GetStudentsDashboardAsync(FacilitatorId);

        var sponsorStatus = result.Students.Single().LetterStatuses.Single();
        sponsorStatus.SponsorId.Should().Be(SponsorId);
        sponsorStatus.RecipientName.Should().Be("Padrino Uno");
        sponsorStatus.IsCompany.Should().BeFalse();
    }

    private void SetupStudentWithOneSponsor() =>
        _facilitatorRepository.GetActiveSponsoredStudentsAsync(FacilitatorId)
            .Returns([
                new FacilitatorStudentsDataModel(Auditable)
                {
                    StudentId = StudentId,
                    StudentFirstName = "Ana",
                    StudentLastName = "Becaria",
                    Sponsors =
                    [
                        new DashboardSponsorDataModel
                        {
                            SponsorshipId = 30,
                            SponsorId = SponsorId,
                            RecipientName = "Padrino Uno",
                            IsCompany = false,
                        },
                    ],
                },
            ]);

    private void SetupStudentWithTwoSponsors() =>
        _facilitatorRepository.GetActiveSponsoredStudentsAsync(FacilitatorId)
            .Returns([
                new FacilitatorStudentsDataModel(Auditable)
                {
                    StudentId = StudentId,
                    StudentFirstName = "Ana",
                    StudentLastName = "Becaria",
                    Sponsors =
                    [
                        new DashboardSponsorDataModel
                        {
                            SponsorshipId = 30,
                            SponsorId = SponsorId,
                            RecipientName = "Padrino Uno",
                            IsCompany = false,
                        },
                        new DashboardSponsorDataModel
                        {
                            SponsorshipId = 31,
                            CompanyId = CompanyId,
                            RecipientName = "Empresa XYZ",
                            IsCompany = true,
                        },
                    ],
                },
            ]);
}