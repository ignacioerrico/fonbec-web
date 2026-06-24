using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.HasKey(a => a.AssessmentId);

        builder.Property(a => a.IssuesNotes)
            .HasMaxLength(Constants.MaxLength.Assessment.IssuesNotes);

        builder.Property(a => a.Appraisal)
            .HasMaxLength(Constants.MaxLength.Assessment.Appraisal);
    }
}

internal class LetterReviewConfiguration : IEntityTypeConfiguration<LetterReview>
{
    public void Configure(EntityTypeBuilder<LetterReview> builder)
    {
        builder.HasKey(r => r.LetterReviewId);

        builder.HasIndex(r => r.DocumentId)
            .IsUnique();

        builder.HasOne(r => r.Document)
            .WithOne(d => d.Review)
            .HasForeignKey<LetterReview>(r => r.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Assessment)
            .WithOne(a => a.LetterReview)
            .HasForeignKey<LetterReview>(r => r.AssessmentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(r => r.ReviewedBy)
            .WithMany()
            .HasForeignKey(r => r.ReviewedById)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

internal class ReportCardReviewConfiguration : IEntityTypeConfiguration<ReportCardReview>
{
    public void Configure(EntityTypeBuilder<ReportCardReview> builder)
    {
        builder.HasKey(r => r.ReportCardReviewId);

        builder.HasIndex(r => r.DocumentId)
            .IsUnique();

        builder.HasOne(r => r.Document)
            .WithOne(d => d.Review)
            .HasForeignKey<ReportCardReview>(r => r.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ReviewedBy)
            .WithMany()
            .HasForeignKey(r => r.ReviewedById)
            .OnDelete(DeleteBehavior.NoAction);
    }
}