using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class FonbecUserConfiguration : IEntityTypeConfiguration<FonbecWebUser>
{
    public void Configure(EntityTypeBuilder<FonbecWebUser> builder)
    {
        builder.Property(u => u.UserName)
            .HasMaxLength(MaxLength.FonbecWebUser.Email);

        builder.Property(u => u.NormalizedUserName)
            .HasMaxLength(MaxLength.FonbecWebUser.Email);

        builder.Property(u => u.Email)
            .HasMaxLength(MaxLength.FonbecWebUser.Email);

        builder.Property(u => u.NormalizedEmail)
            .HasMaxLength(MaxLength.FonbecWebUser.Email);

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(MaxLength.FonbecWebUser.FirstName);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(MaxLength.FonbecWebUser.LastName);

        builder.Property(u => u.NickName)
            .HasMaxLength(MaxLength.FonbecWebUser.NickName);

        builder.Property(u => u.Notes)
            .HasMaxLength(MaxLength.FonbecWebUser.Notes);

        builder.HasOne(u => u.Chapter)
            .WithMany()
            .HasForeignKey(u => u.ChapterId)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit Properties

        builder.HasOne(u => u.CreatedBy)
            .WithOne()
            .HasForeignKey<FonbecWebUser>(u => u.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(u => u.CreatedOnUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasOne(u => u.LastUpdatedBy)
            .WithOne()
            .HasForeignKey<FonbecWebUser>(u => u.LastUpdatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.DisabledBy)
            .WithOne()
            .HasForeignKey<FonbecWebUser>(u => u.DisabledById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.ReenabledBy)
            .WithOne()
            .HasForeignKey<FonbecWebUser>(u => u.ReenabledById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}