using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Logic.Tests.Models;
using Mapster;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class FacilitatorServiceOtherDocumentsTests : MappingTestBase
{
    private readonly IFacilitatorRepository _facilitatorRepository;
    private readonly FacilitatorService _facilitatorService;

    public FacilitatorServiceOtherDocumentsTests()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(FacilitatorService).Assembly);

        _facilitatorRepository = Substitute.For<IFacilitatorRepository>();
        var letterExemptionService = Substitute.For<ILetterExemptionService>();
        letterExemptionService
            .GetActiveExemptionReasonsForPlanAsync(Arg.Any<int>())
            .Returns(new Dictionary<int, string>());

        _facilitatorRepository
            .GetLatestReportCardsAsync(Arg.Any<List<int>>(), Arg.Any<int>())
            .Returns([]);
        _facilitatorRepository
            .GetOtherDocumentCountsAsync(Arg.Any<List<int>>())
            .Returns([]);

        _facilitatorService = new FacilitatorService(_facilitatorRepository, letterExemptionService);
    }

    [Fact]
    public async Task GetStudentsDashboardAsync_Assigns_Other_Document_Counts()
    {
        _facilitatorRepository.GetActiveSponsoredStudentsAsync(2)
            .Returns([
                new FacilitatorStudentsDataModel(Auditable)
                {
                    StudentId = 10,
                    StudentFirstName = "María",
                    StudentLastName = "González",
                },
                new FacilitatorStudentsDataModel(Auditable)
                {
                    StudentId = 11,
                    StudentFirstName = "Ana",
                    StudentLastName = "Becaria",
                },
            ]);

        _facilitatorRepository.GetOtherDocumentCountsAsync(Arg.Any<List<int>>())
            .Returns(new Dictionary<int, int> { [10] = 3 });

        var result = await _facilitatorService.GetStudentsDashboardAsync(2);

        var maria = result.Students.Single(s => s.StudentId == 10);
        maria.OtherDocuments.Count.Should().Be(3);
        maria.OtherDocuments.HasAny.Should().BeTrue();

        var ana = result.Students.Single(s => s.StudentId == 11);
        ana.OtherDocuments.Count.Should().Be(0);
        ana.OtherDocuments.HasAny.Should().BeFalse();
    }

    [Fact]
    public async Task GetOtherDocumentsHistoryAsync_Maps_FileKind_Status_Reason_And_Download_Eligibility()
    {
        _facilitatorRepository.GetOtherDocumentsHistoryAsync(2, 10)
            .Returns(new FacilitatorOtherDocumentsHistoryDataModel
            {
                StudentId = 10,
                StudentName = "María González",
                Items =
                [
                    new FacilitatorOtherDocumentItemDataModel
                    {
                        DocumentId = 5,
                        UploadedOn = new DateTime(2026, 6, 12, 15, 0, 0, DateTimeKind.Utc),
                        Description = "Certificado de alumno",
                        FileKind = FileKind.Blob,
                        Status = DocumentStatus.Approved,
                        PageCount = 1,
                    },
                    new FacilitatorOtherDocumentItemDataModel
                    {
                        DocumentId = 4,
                        UploadedOn = new DateTime(2026, 5, 3, 10, 0, 0, DateTimeKind.Utc),
                        Description = "Nota de la familia",
                        FileKind = FileKind.Text,
                        Status = DocumentStatus.ReviewPending,
                        TextContent = "Texto de la nota",
                        PageCount = 0,
                    },
                    new FacilitatorOtherDocumentItemDataModel
                    {
                        DocumentId = 3,
                        UploadedOn = new DateTime(2026, 3, 21, 8, 0, 0, DateTimeKind.Utc),
                        Description = "Foto del acto escolar",
                        FileKind = FileKind.Blob,
                        Status = DocumentStatus.Rejected,
                        RejectionReason = "Imagen borrosa: falta el sello",
                        PageCount = 2,
                    },
                    new FacilitatorOtherDocumentItemDataModel
                    {
                        DocumentId = 2,
                        UploadedOn = new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc),
                        Description = "Acto escolar",
                        FileKind = FileKind.YouTube,
                        Status = DocumentStatus.Approved,
                        YouTubeVideoId = "abc123",
                        PageCount = 0,
                    },
                    new FacilitatorOtherDocumentItemDataModel
                    {
                        DocumentId = 1,
                        UploadedOn = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc),
                        Description = "Archivo sin páginas",
                        FileKind = FileKind.Blob,
                        Status = DocumentStatus.Approved,
                        PageCount = 0,
                    },
                ],
            });

        var result = await _facilitatorService.GetOtherDocumentsHistoryAsync(2, 10);

        result.Should().NotBeNull();
        result!.StudentId.Should().Be(10);
        result.StudentName.Should().Be("María González");
        result.Items.Select(i => i.DocumentId).Should().Equal(5, 4, 3, 2, 1);

        var file = result.Items[0];
        file.Date.Should().Be(new DateOnly(2026, 6, 12));
        file.Description.Should().Be("Certificado de alumno");
        file.FileKind.Should().Be(FileKind.Blob);
        file.Status.Should().Be(DocumentStatus.Approved);
        file.CanDownload.Should().BeTrue();

        var text = result.Items[1];
        text.FileKind.Should().Be(FileKind.Text);
        text.Status.Should().Be(DocumentStatus.ReviewPending);
        text.TextContent.Should().Be("Texto de la nota");
        text.CanDownload.Should().BeFalse();

        var rejected = result.Items[2];
        rejected.Status.Should().Be(DocumentStatus.Rejected);
        rejected.RejectionReason.Should().Be("Imagen borrosa: falta el sello");
        rejected.CanDownload.Should().BeTrue();
        rejected.PageCount.Should().Be(2);

        result.Items[3].FileKind.Should().Be(FileKind.YouTube);
        result.Items[3].YouTubeVideoId.Should().Be("abc123");
        result.Items[3].CanDownload.Should().BeFalse();

        result.Items[4].CanDownload.Should().BeFalse();
    }

    [Fact]
    public async Task GetOtherDocumentsHistoryAsync_Returns_Null_When_Student_Is_Not_The_Facilitators()
    {
        _facilitatorRepository.GetOtherDocumentsHistoryAsync(2, 99)
            .Returns((FacilitatorOtherDocumentsHistoryDataModel?)null);

        var result = await _facilitatorService.GetOtherDocumentsHistoryAsync(2, 99);

        result.Should().BeNull();
    }
}