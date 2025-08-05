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
    }
}