using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class DocumentShareConfiguration : IEntityTypeConfiguration<DocumentShare>
{
    public void Configure(EntityTypeBuilder<DocumentShare> builder)
    {
        builder.HasKey(s => s.DocumentShareId);

        builder.HasIndex(s => new { s.DocumentId, s.SponsorId })
            .IsUnique();

        builder.HasOne(s => s.Document)
            .WithMany(d => d.Shares)
            .HasForeignKey(s => s.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Sponsor)
            .WithMany()
            .HasForeignKey(s => s.SponsorId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(s => s.Student)
            .WithMany()
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(s => s.SharedBy)
            .WithMany()
            .HasForeignKey(s => s.SharedById)
            .OnDelete(DeleteBehavior.NoAction);
    }
}