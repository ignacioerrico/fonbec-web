using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Documents;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Documents;

public class StudentDocumentHistoryMappingTests : MappingTestBase
{
    [Fact]
    public void Maps_Letter_ReportCard_And_Other_Details()
    {
        var history = new StudentDocumentHistoryDataModel
        {
            StudentId = 10,
            StudentName = "María González",
            Items =
            [
                new StudentDocumentHistoryItemDataModel
                {
                    DocumentId = 1,
                    DocumentType = DocumentType.Letter,
                    UploadedOn = new DateTime(2026, 3, 21),
                    RecipientName = "Ana Pérez",
                    FileKind = FileKind.Text,
                    Status = DocumentStatus.Approved,
                },
                new StudentDocumentHistoryItemDataModel
                {
                    DocumentId = 2,
                    DocumentType = DocumentType.ReportCard,
                    UploadedOn = new DateTime(2026, 6, 1),
                    Description = "Boletín 1º trimestre",
                    ReportCardPeriod = new DateOnly(2026, 6, 1),
                    FileKind = FileKind.Blob,
                    Status = DocumentStatus.ReviewPending,
                    PageCount = 2,
                },
                new StudentDocumentHistoryItemDataModel
                {
                    DocumentId = 3,
                    DocumentType = DocumentType.Other,
                    UploadedOn = new DateTime(2026, 5, 3),
                    Description = "Foto del acto",
                    FileKind = FileKind.Blob,
                    Status = DocumentStatus.Rejected,
                    PageCount = 0,
                },
            ],
        };

        var viewModel = history.Adapt<StudentDocumentHistoryViewModel>(Config);

        viewModel.StudentName.Should().Be("María González");
        viewModel.Items.Should().HaveCount(3);

        var letter = viewModel.Items.Single(i => i.DocumentId == 1);
        letter.Detail.Should().Be("Ana Pérez");
        letter.Date.Should().Be(new DateOnly(2026, 3, 21));
        letter.CanDownload.Should().BeFalse();

        var reportCard = viewModel.Items.Single(i => i.DocumentId == 2);
        reportCard.Detail.Should().Be("junio de 2026 — Boletín 1º trimestre");
        reportCard.CanDownload.Should().BeTrue();

        var other = viewModel.Items.Single(i => i.DocumentId == 3);
        other.Detail.Should().Be("Foto del acto");
        other.CanDownload.Should().BeFalse();
    }
}