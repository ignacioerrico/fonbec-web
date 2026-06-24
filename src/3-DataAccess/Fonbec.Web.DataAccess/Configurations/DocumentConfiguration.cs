using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(d => d.DocumentId);

        builder.HasDiscriminator(d => d.DocumentType)
            .HasValue<Letter>(DocumentType.Letter)
            .HasValue<ReportCard>(DocumentType.ReportCard)
            .HasValue<OtherDocument>(DocumentType.Other);

        builder.Property(d => d.YouTubeVideoId)
            .HasMaxLength(Constants.MaxLength.Document.YouTubeVideoId);

        builder.Property(d => d.TextContent)
            .HasMaxLength(Constants.MaxLength.Document.TextContent);

        builder.Property(d => d.UploaderNotes)
            .HasMaxLength(Constants.MaxLength.Document.UploaderNotes);

        builder.Property(d => d.RejectionNotes)
            .HasMaxLength(Constants.MaxLength.Document.RejectionNotes);

        builder.Property(d => d.RowVersion)
            .IsRowVersion();

        builder.HasOne(d => d.Chapter)
            .WithMany()
            .HasForeignKey(d => d.ChapterId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(d => d.Student)
            .WithMany()
            .HasForeignKey(d => d.StudentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(d => d.Sponsor)
            .WithMany()
            .HasForeignKey(d => d.SponsorId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(d => d.BlobPath)
            .WithMany()
            .HasForeignKey(d => d.BlobPathId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(d => d.OriginalBlobPath)
            .WithMany()
            .HasForeignKey(d => d.OriginalBlobPathId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(d => d.ImprovedBlobPath)
            .WithMany()
            .HasForeignKey(d => d.ImprovedBlobPathId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(d => d.UploadedBy)
            .WithMany()
            .HasForeignKey(d => d.UploadedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(d => d.ImprovementLockedBy)
            .WithMany()
            .HasForeignKey(d => d.ImprovementLockedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(d => d.RejectedReason)
            .WithMany()
            .HasForeignKey(d => d.RejectedReasonId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(d => new { d.Status, d.UploadedOn });

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_Document_ApprovedOrRejected",
                "[ApprovedOn] IS NULL OR [RejectedOn] IS NULL");

            t.HasCheckConstraint(
                "CK_Document_ImprovementComplete",
                "[DigitalImprovementStatus] <> 3 OR [ImprovedBlobPathId] IS NOT NULL");

            t.HasCheckConstraint(
                "CK_Document_ImprovementNotApplicable",
                "[DigitalImprovementStatus] <> 0 OR [ImprovedBlobPathId] IS NULL");
        });
    }
}

internal class LetterConfiguration : IEntityTypeConfiguration<Letter>
{
    public void Configure(EntityTypeBuilder<Letter> builder)
    {
        builder.HasOne(l => l.Plan)
            .WithMany()
            .HasForeignKey(l => l.PlanId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(l => new { l.StudentId, l.SponsorId, l.PlanId })
            .IsUnique()
            .HasFilter($"[{nameof(Document.DocumentType)}] = {(byte)DocumentType.Letter} AND [{nameof(Document.Status)}] <> {(byte)DocumentStatus.Rejected}");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_Letter_SponsorRequired",
                $"[{nameof(Document.DocumentType)}] <> {(byte)DocumentType.Letter} OR [{nameof(Document.SponsorId)}] IS NOT NULL");
        });
    }
}

internal class ReportCardConfiguration : IEntityTypeConfiguration<ReportCard>
{
    public void Configure(EntityTypeBuilder<ReportCard> builder)
    {
        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_ReportCard_SponsorNull",
                $"[{nameof(Document.DocumentType)}] <> {(byte)DocumentType.ReportCard} OR [{nameof(Document.SponsorId)}] IS NULL");
        });
    }
}

internal class OtherDocumentConfiguration : IEntityTypeConfiguration<OtherDocument>
{
    public void Configure(EntityTypeBuilder<OtherDocument> builder)
    {
        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_OtherDocument_SponsorNull",
                $"[{nameof(Document.DocumentType)}] <> {(byte)DocumentType.Other} OR [{nameof(Document.SponsorId)}] IS NULL");
        });
    }
}