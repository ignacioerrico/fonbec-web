using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class LetterExemptionConfiguration : IEntityTypeConfiguration<LetterExemption>
{
    public void Configure(EntityTypeBuilder<LetterExemption> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Reason)
            .IsRequired()
            .HasMaxLength(Constants.MaxLength.LetterExemption.Reason);

        // At most one active (non-revoked) exemption per student + plan.
        builder.HasIndex(e => new { e.StudentId, e.PlannedDeliveryId })
            .IsUnique()
            .HasFilter($"[{nameof(LetterExemption.IsRevoked)}] = 0");

        builder.HasIndex(e => new { e.PlannedDeliveryId, e.ChapterId, e.IsRevoked });

        builder.HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.PlannedDelivery)
            .WithMany()
            .HasForeignKey(e => e.PlannedDeliveryId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<Chapter>()
            .WithMany()
            .HasForeignKey(e => e.ChapterId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<FonbecWebUser>()
            .WithMany()
            .HasForeignKey(e => e.CreatedByFonbecUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<FonbecWebUser>()
            .WithMany()
            .HasForeignKey(e => e.RevokedByFonbecUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}