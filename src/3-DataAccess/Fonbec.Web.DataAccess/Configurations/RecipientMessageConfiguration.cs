using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class RecipientMessageConfiguration : IEntityTypeConfiguration<RecipientMessage>
{
    public void Configure(EntityTypeBuilder<RecipientMessage> builder)
    {
        builder.HasKey(m => m.RecipientMessageId);

        builder.Property(m => m.Body)
            .IsRequired()
            .HasMaxLength(Constants.MaxLength.RecipientMessage.Body);

        builder.HasIndex(m => new { m.SponsorId, m.StudentId, m.SentOn });
        builder.HasIndex(m => new { m.CompanyId, m.StudentId, m.SentOn });

        builder.HasOne(m => m.Student)
            .WithMany()
            .HasForeignKey(m => m.StudentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(m => m.Sponsor)
            .WithMany()
            .HasForeignKey(m => m.SponsorId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(m => m.Company)
            .WithMany()
            .HasForeignKey(m => m.CompanyId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(m => m.SharedBy)
            .WithMany()
            .HasForeignKey(m => m.SharedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_RecipientMessage_RecipientRequired",
            $"([{nameof(RecipientMessage.SponsorId)}] IS NOT NULL AND [{nameof(RecipientMessage.CompanyId)}] IS NULL) "
            + $"OR ([{nameof(RecipientMessage.SponsorId)}] IS NULL AND [{nameof(RecipientMessage.CompanyId)}] IS NOT NULL)"));
    }
}