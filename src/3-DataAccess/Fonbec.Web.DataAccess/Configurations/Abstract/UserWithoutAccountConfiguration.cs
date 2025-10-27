using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations.Abstract;

internal class UserWithoutAccountConfiguration<T> : AuditableEntityTypeConfiguration<T> where T : UserWithoutAccount
{
    public override void Configure(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(MaxLength.FonbecWebUser.FirstName);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(MaxLength.FonbecWebUser.LastName);

        builder.Property(u => u.NickName)
            .IsRequired(false)
            .HasMaxLength(MaxLength.FonbecWebUser.NickName);

        builder.Property(u => u.PhoneNumber)
            .IsRequired(false)
            .HasMaxLength(MaxLength.FonbecWebUser.PhoneNumber);

        builder.Property(s => s.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasOne(u => u.Chapter)
            .WithMany()
            .HasForeignKey(u => u.ChapterId)
            .OnDelete(DeleteBehavior.NoAction);

        base.Configure(builder);
    }
}