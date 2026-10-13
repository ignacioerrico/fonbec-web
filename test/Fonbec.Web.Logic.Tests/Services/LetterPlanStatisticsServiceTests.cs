using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.LetterPlanStatistics;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Services;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class LetterPlanStatisticsServiceTests
{
    private static readonly DateTime UploadedOn = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);

    private readonly ILetterPlanStatisticsRepository _repository = Substitute.For<ILetterPlanStatisticsRepository>();
    private readonly LetterPlanStatisticsService _service;

    public LetterPlanStatisticsServiceTests()
    {
        _service = new LetterPlanStatisticsService(_repository);
    }

    [Fact]
    public async Task GetStatisticsAsync_Returns_Null_When_Repository_Returns_Null()
    {
        _repository.GetStatisticsAsync(1, 2).Returns((LetterPlanStatisticsQueryResultDataModel?)null);

        var result = await _service.GetStatisticsAsync(1, 2);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Map_Buckets_Written_Date_To_Addressee_Notification_And_Counts_Unnotified()
    {
        var result = await MapAsync(Source(
            Approved(written: new DateTime(2026, 3, 1), notified: new DateTime(2026, 3, 1, 18, 0, 0, DateTimeKind.Utc)),
            Approved(written: new DateTime(2026, 3, 1), notified: new DateTime(2026, 3, 6, 8, 0, 0, DateTimeKind.Utc)),
            Approved(written: new DateTime(2026, 3, 1), notified: new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc)),
            Approved(written: new DateTime(2026, 3, 1), notified: null)));

        result.WrittenToNotification.Labels[0].Should().Be("Menos de 1 día");
        result.WrittenToNotification.Counts.Should().Equal(1, 0, 1, 0, 0, 1);
        result.WrittenToNotification.MedianText.Should().Be("Mediana: 5 días");
        result.WrittenToNotification.AverageText.Should().Be("Promedio: 22 días");
        result.WrittenToNotification.MinText.Should().Be("Mínimo: menos de 1 día");
        result.WrittenToNotification.MaxText.Should().Be("Máximo: 61 días");
        result.WrittenToNotification.Note.Should().Be("1 carta sin aviso al padrino.");
    }

    [Fact]
    public async Task Map_Splits_Image_Waits_And_Omits_Pdfs_From_Improvement()
    {
        var image = Letter(
            DigitalImprovementStatus.Complete,
            DocumentStatus.Approved,
            improvementFirst: UploadedOn.AddDays(2),
            improvementCompleted: UploadedOn.AddDays(2).AddMinutes(20),
            reviewFirst: UploadedOn.AddDays(5));
        var pdf = Letter(
            DigitalImprovementStatus.NotApplicable,
            DocumentStatus.Approved,
            reviewFirst: UploadedOn.AddHours(12));
        var imageWithoutStart = Letter(DigitalImprovementStatus.Required, DocumentStatus.Rejected);

        var result = await MapAsync(Source(image, pdf, imageWithoutStart));

        result.WaitForImprovement.Counts[1].Should().Be(1);
        result.WaitForImprovement.Counts.Sum().Should().Be(1);
        result.WaitForImprovement.Note.Should().Be("1 carta sin este dato.");

        result.WaitForReview.Counts[0].Should().Be(1);
        result.WaitForReview.Counts[1].Should().Be(1);
        result.WaitForReview.Note.Should().Be("1 carta sin este dato.");
    }

    [Fact]
    public async Task Map_Uses_The_Lock_That_Finished_Improvement_And_Review()
    {
        var image = Letter(
            DigitalImprovementStatus.Complete,
            DocumentStatus.Approved,
            decisionImprovement: UploadedOn.AddMinutes(10),
            improvementCompleted: UploadedOn.AddMinutes(25),
            decisionReview: UploadedOn.AddHours(3),
            reviewedOn: UploadedOn.AddHours(3).AddMinutes(12),
            reviewerName: "Ana Pérez",
            spelling: 5,
            penmanship: 4,
            content: 5);
        var historicalImage = Letter(DigitalImprovementStatus.Complete, DocumentStatus.Approved);
        var rejected = Letter(
            DigitalImprovementStatus.NotApplicable,
            DocumentStatus.Rejected,
            decisionReview: UploadedOn,
            rejectedOn: UploadedOn.AddMinutes(40),
            rejectedByName: "Luis Gómez",
            rejectionReason: "Ilegible");

        var result = await MapAsync(Source(image, historicalImage, rejected));

        result.ImprovementDuration.Counts[1].Should().Be(1);
        result.ImprovementDuration.MedianText.Should().Be("Mediana: 15 min");
        result.ImprovementDuration.AverageText.Should().Be("Promedio: 15 min");
        result.ImprovementDuration.MinText.Should().Be("Mínimo: 15 min");
        result.ImprovementDuration.MaxText.Should().Be("Máximo: 15 min");
        result.ImprovementDuration.Note.Should().Be("1 carta sin este dato.");

        result.ReviewDuration.Counts[1].Should().Be(1);
        result.ReviewDuration.Counts[3].Should().Be(1);
        result.ReviewDuration.Note.Should().Be("1 carta sin este dato.");

        result.RejectionsByReviewer.Labels.Should().Equal("Luis Gómez");
        result.RejectionsByReviewer.Counts.Should().Equal(1);
        result.ReviewerActivity.Labels.Should().Equal("Ana Pérez", "Luis Gómez");
        result.ReviewerActivity.Approvals.Should().Equal(1, 0);
        result.ReviewerActivity.Rejections.Should().Equal(0, 1);
        result.RejectionReasons.Labels.Should().Equal("Ilegible");
        result.Scores.Spelling[4].Should().Be(1);
        result.Scores.Penmanship[3].Should().Be(1);
        result.Scores.Content[4].Should().Be(1);
    }

    [Fact]
    public async Task Map_Counts_Approved_Rejected_Attempts_And_Exemptions()
    {
        var source = Source(
            Letter(DigitalImprovementStatus.NotApplicable, DocumentStatus.Approved),
            Letter(DigitalImprovementStatus.NotApplicable, DocumentStatus.Rejected, rejectionReason: "Otro"),
            Letter(DigitalImprovementStatus.NotApplicable, DocumentStatus.Rejected));
        source.ExemptionCount = 2;

        var result = await MapAsync(source);

        result.Outcomes.Labels.Should().Equal("Aprobadas", "Rechazadas", "Eximidas");
        result.Outcomes.Counts.Should().Equal(1, 2, 2);
        result.RejectionReasons.Labels.Should().Equal("Otro", "Sin motivo");
        result.RejectionsByReviewer.HasData.Should().BeFalse();
        result.RejectionsByReviewer.Note.Should().Be("2 rechazos sin revisor registrado.");
    }

    private async Task<Fonbec.Web.Logic.Models.LetterPlanStatistics.LetterPlanStatisticsViewModel> MapAsync(
        LetterPlanStatisticsQueryResultDataModel source)
    {
        _repository.GetStatisticsAsync(7, 3).Returns(source);
        return (await _service.GetStatisticsAsync(7, 3))!;
    }

    private static LetterPlanStatisticsQueryResultDataModel Source(
        params LetterPlanStatisticsLetterDataModel[] letters) =>
        new()
        {
            PlanStartsOn = new DateTime(2026, 3, 1),
            Letters = letters.ToList(),
        };

    private static LetterPlanStatisticsLetterDataModel Approved(DateTime written, DateTime? notified) =>
        new()
        {
            Status = DocumentStatus.Approved,
            DigitalImprovementStatus = DigitalImprovementStatus.NotApplicable,
            UploadedOn = UploadedOn,
            ConfirmedWrittenDate = written,
            AddresseeNotificationSentOn = notified,
            ReviewFirstLockedOn = UploadedOn,
        };

    private static LetterPlanStatisticsLetterDataModel Letter(
        DigitalImprovementStatus improvement,
        DocumentStatus status,
        DateTime? improvementFirst = null,
        DateTime? decisionImprovement = null,
        DateTime? improvementCompleted = null,
        DateTime? reviewFirst = null,
        DateTime? decisionReview = null,
        DateTime? reviewedOn = null,
        string? reviewerName = null,
        DateTime? rejectedOn = null,
        string? rejectedByName = null,
        string? rejectionReason = null,
        int? spelling = null,
        int? penmanship = null,
        int? content = null) =>
        new()
        {
            DigitalImprovementStatus = improvement,
            Status = status,
            UploadedOn = UploadedOn,
            ImprovementFirstLockedOn = improvementFirst,
            DecisionImprovementLockedOn = decisionImprovement,
            ImprovementCompletedOn = improvementCompleted,
            ReviewFirstLockedOn = reviewFirst,
            DecisionReviewLockedOn = decisionReview,
            ReviewedOn = reviewedOn,
            ReviewerName = reviewerName,
            RejectedOn = rejectedOn,
            RejectedByName = rejectedByName,
            RejectionReason = rejectionReason,
            SpellingScore = spelling,
            PenmanshipScore = penmanship,
            ContentScore = content,
        };
}