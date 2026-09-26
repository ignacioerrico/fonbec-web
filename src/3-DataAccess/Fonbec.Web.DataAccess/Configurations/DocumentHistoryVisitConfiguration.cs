using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class DocumentHistoryVisitConfiguration : IEntityTypeConfiguration<DocumentHistoryVisit>
{
    public void Configure(EntityTypeBuilder<DocumentHistoryVisit> builder)
    {
        builder.HasKey(v => v.DocumentHistoryVisitId);

        builder.HasIndex(v => new { v.SponsorId, v.StudentId })
            .IsUnique()
            .HasFilter($"[{nameof(DocumentHistoryVisit.SponsorId)}] IS NOT NULL");

        builder.HasIndex(v => new { v.CompanyId, v.StudentId })
            .IsUnique()
            .HasFilter($"[{nameof(DocumentHistoryVisit.CompanyId)}] IS NOT NULL");

        builder.HasOne(v => v.Sponsor)
            .WithMany()
            .HasForeignKey(v => v.SponsorId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(v => v.Company)
            .WithMany()
            .HasForeignKey(v => v.CompanyId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(v => v.Student)
            .WithMany()
            .HasForeignKey(v => v.StudentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_DocumentHistoryVisit_RecipientRequired",
            $"([{nameof(DocumentHistoryVisit.SponsorId)}] IS NOT NULL AND [{nameof(DocumentHistoryVisit.CompanyId)}] IS NULL) "
            + $"OR ([{nameof(DocumentHistoryVisit.SponsorId)}] IS NULL AND [{nameof(DocumentHistoryVisit.CompanyId)}] IS NOT NULL)"));
    }
}