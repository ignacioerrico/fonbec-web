using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Logic.Tests.Models;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class FacilitatorServiceStudentHistoryTests : MappingTestBase
{
    private readonly IFacilitatorRepository _facilitatorRepository;
    private readonly FacilitatorService _facilitatorService;

    public FacilitatorServiceStudentHistoryTests()
    {
        _facilitatorRepository = Substitute.For<IFacilitatorRepository>();
        var letterExemptionService = Substitute.For<ILetterExemptionService>();
        _facilitatorService = new FacilitatorService(_facilitatorRepository, letterExemptionService);
    }

    [Fact]
    public async Task GetLettersHistoryAsync_Maps_Campaign_Recipient_Kind_And_Hides_Unreturned_Reasons()
    {
        _facilitatorRepository.GetLettersHistoryAsync(2, 10)
            .Returns(new FacilitatorLettersHistoryDataModel
            {
                StudentId = 10,
                StudentName = "María González",
                Items =
                [
                    new FacilitatorLetterHistoryItemDataModel
                    {
                        DocumentId = 5,
                        UploadedOn = new DateTime(2026, 6, 12, 15, 0, 0, DateTimeKind.Utc),
                        PlanStartsOn = new DateTime(2026, 6, 1),
                        RecipientName = "Luis Padrino",
                        FileKind = FileKind.Blob,
                        Status = DocumentStatus.Approved,
                        PageCount = 1,
                    },
                    new FacilitatorLetterHistoryItemDataModel
                    {
                        DocumentId = 4,
                        UploadedOn = new DateTime(2026, 5, 3, 10, 0, 0, DateTimeKind.Utc),
                        PlanStartsOn = new DateTime(2026, 5, 1),
                        RecipientName = "Acme",
                        FileKind = FileKind.Text,
                        Status = DocumentStatus.ReviewPending,
                        TextContent = "Texto de la carta",
                    },
                    new FacilitatorLetterHistoryItemDataModel
                    {
                        DocumentId = 3,
                        UploadedOn = new DateTime(2026, 3, 21, 8, 0, 0, DateTimeKind.Utc),
                        PlanStartsOn = new DateTime(2026, 3, 1),
                        RecipientName = "Luis Padrino",
                        FileKind = FileKind.Blob,
                        Status = DocumentStatus.Rejected,
                        RejectionReason = "Carta muy corta: falta el cierre",
                        PageCount = 2,
                    },
                    new FacilitatorLetterHistoryItemDataModel
                    {
                        DocumentId = 2,
                        UploadedOn = new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc),
                        PlanStartsOn = new DateTime(2026, 2, 1),
                        RecipientName = "Acme",
                        FileKind = FileKind.YouTube,
                        Status = DocumentStatus.Approved,
                        YouTubeVideoId = "abc123",
                    },
                    new FacilitatorLetterHistoryItemDataModel
                    {
                        DocumentId = 1,
                        UploadedOn = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc),
                        PlanStartsOn = new DateTime(2026, 1, 1),
                        RecipientName = "Luis Padrino",
                        FileKind = FileKind.Blob,
                        Status = DocumentStatus.Approved,
                    },
                    new FacilitatorLetterHistoryItemDataModel
                    {
                        DocumentId = 6,
                        UploadedOn = new DateTime(2026, 3, 22, 8, 0, 0, DateTimeKind.Utc),
                        PlanStartsOn = new DateTime(2026, 3, 1),
                        RecipientName = "Luis Padrino",
                        FileKind = FileKind.Text,
                        Status = DocumentStatus.PendingManagerDecision,
                        RejectionReason = "Ilegible",
                        TextContent = "Texto",
                    },
                ],
            });

        var result = await _facilitatorService.GetLettersHistoryAsync(2, 10);

        result.Should().NotBeNull();
        result!.StudentName.Should().Be("María González");
        result.Items.Select(i => i.DocumentId).Should().Equal(5, 4, 3, 2, 1, 6);

        var file = result.Items[0];
        file.Date.Should().Be(new DateOnly(2026, 6, 12));
        file.CampaignLabel.Should().Be(new DateTime(2026, 6, 1).ToSpanishMonthYear());
        file.RecipientName.Should().Be("Luis Padrino");
        file.CanDownload.Should().BeTrue();

        result.Items[1].FileKind.Should().Be(FileKind.Text);
        result.Items[1].TextContent.Should().Be("Texto de la carta");
        result.Items[1].CanDownload.Should().BeFalse();

        var rejected = result.Items[2];
        rejected.Status.Should().Be(DocumentStatus.Rejected);
        rejected.RejectionReason.Should().Be("Carta muy corta: falta el cierre");
        rejected.PageCount.Should().Be(2);

        result.Items[3].YouTubeVideoId.Should().Be("abc123");
        result.Items[3].CanDownload.Should().BeFalse();
        result.Items[4].CanDownload.Should().BeFalse();

        var held = result.Items[5];
        held.Status.Should().Be(DocumentStatus.PendingManagerDecision);
        held.RejectionReason.Should().BeNull();
    }

    [Fact]
    public async Task GetLettersHistoryAsync_Returns_Null_When_Student_Is_Not_The_Facilitators()
    {
        _facilitatorRepository.GetLettersHistoryAsync(2, 99)
            .Returns((FacilitatorLettersHistoryDataModel?)null);

        var result = await _facilitatorService.GetLettersHistoryAsync(2, 99);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetReportCardsHistoryAsync_Maps_Period_Level_And_Hides_Unreturned_Reasons()
    {
        _facilitatorRepository.GetReportCardsHistoryAsync(2, 10)
            .Returns(new FacilitatorReportCardsHistoryDataModel
            {
                StudentId = 10,
                StudentName = "María González",
                EducationLevel = EducationLevel.University,
                Items =
                [
                    new FacilitatorReportCardHistoryItemDataModel
                    {
                        DocumentId = 3,
                        Period = new DateOnly(2026, 6, 1),
                        Description = "Primer cuatrimestre",
                        UploadedOn = new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc),
                        FileKind = FileKind.Blob,
                        Status = DocumentStatus.Approved,
                        PageCount = 1,
                    },
                    new FacilitatorReportCardHistoryItemDataModel
                    {
                        DocumentId = 2,
                        Period = new DateOnly(2026, 3, 1),
                        Description = "Recuperatorio",
                        UploadedOn = new DateTime(2026, 4, 2, 8, 0, 0, DateTimeKind.Utc),
                        FileKind = FileKind.Blob,
                        Status = DocumentStatus.Rejected,
                        RejectionReason = "Imagen borrosa: falta el sello",
                        PageCount = 2,
                    },
                    new FacilitatorReportCardHistoryItemDataModel
                    {
                        DocumentId = 1,
                        Period = new DateOnly(2026, 2, 1),
                        Description = "En espera",
                        UploadedOn = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc),
                        FileKind = FileKind.YouTube,
                        Status = DocumentStatus.PendingManagerDecision,
                        RejectionReason = "Ilegible",
                        YouTubeVideoId = "libreta1",
                    },
                ],
            });

        var result = await _facilitatorService.GetReportCardsHistoryAsync(2, 10);

        result.Should().NotBeNull();
        result!.EducationLevel.Should().Be(EducationLevel.University);
        result.Items.Select(i => i.DocumentId).Should().Equal(3, 2, 1);

        var latest = result.Items[0];
        latest.Period.Should().Be(new DateOnly(2026, 6, 1));
        latest.PeriodLabel.Should().Be(new DateOnly(2026, 6, 1).ToString("MMM/yy"));
        latest.Description.Should().Be("Primer cuatrimestre");
        latest.Date.Should().Be(new DateOnly(2026, 7, 1));
        latest.CanDownload.Should().BeTrue();

        var rejected = result.Items[1];
        rejected.RejectionReason.Should().Be("Imagen borrosa: falta el sello");
        rejected.CanDownload.Should().BeTrue();

        var held = result.Items[2];
        held.YouTubeVideoId.Should().Be("libreta1");
        held.CanDownload.Should().BeFalse();
        held.RejectionReason.Should().BeNull();
    }

    [Fact]
    public async Task GetReportCardsHistoryAsync_Returns_Null_When_Student_Is_Not_The_Facilitators()
    {
        _facilitatorRepository.GetReportCardsHistoryAsync(2, 99)
            .Returns((FacilitatorReportCardsHistoryDataModel?)null);

        var result = await _facilitatorService.GetReportCardsHistoryAsync(2, 99);

        result.Should().BeNull();
    }
}