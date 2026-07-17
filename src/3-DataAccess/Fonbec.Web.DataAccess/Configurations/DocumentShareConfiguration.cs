using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class DocumentShareConfiguration : IEntityTypeConfiguration<DocumentShare>
{
    public void Configure(EntityTypeBuilder<DocumentShare> builder)
    {
        builder.HasKey(s => s.DocumentShareId);

        // A document is shared at most once per recipient. Person and company recipients live in
        // separate filtered unique indexes so the NULL side never collides on SQL Server.
        builder.HasIndex(s => new { s.DocumentId, s.SponsorId })
            .IsUnique()
            .HasFilter($"[{nameof(DocumentShare.SponsorId)}] IS NOT NULL");

        builder.HasIndex(s => new { s.DocumentId, s.CompanyId })
            .IsUnique()
            .HasFilter($"[{nameof(DocumentShare.CompanyId)}] IS NOT NULL");

        builder.HasOne(s => s.Document)
            .WithMany(d => d.Shares)
            .HasForeignKey(s => s.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Sponsor)
            .WithMany()
            .HasForeignKey(s => s.SponsorId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(s => s.Company)
            .WithMany()
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(s => s.Student)
            .WithMany()
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(s => s.SharedBy)
            .WithMany()
            .HasForeignKey(s => s.SharedById)
            .OnDelete(DeleteBehavior.NoAction);

        // A share is addressed to exactly one recipient: a sponsor XOR a company.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_DocumentShare_RecipientRequired",
            $"([{nameof(DocumentShare.SponsorId)}] IS NOT NULL AND [{nameof(DocumentShare.CompanyId)}] IS NULL) "
            + $"OR ([{nameof(DocumentShare.SponsorId)}] IS NULL AND [{nameof(DocumentShare.CompanyId)}] IS NOT NULL)"));
    }
}