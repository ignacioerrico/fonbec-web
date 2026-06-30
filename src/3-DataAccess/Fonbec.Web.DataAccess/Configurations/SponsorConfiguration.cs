using Fonbec.Web.DataAccess.Configurations.Abstract;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class SponsorConfiguration : UserWithoutAccountConfiguration<Sponsor>
{
    public override void Configure(EntityTypeBuilder<Sponsor> builder)
    {
        builder.Property(s => s.Email)
            .IsRequired(false)
            .HasMaxLength(Constants.MaxLength.FonbecWebUser.Email);

        builder.Property(s => s.Notes)
            .IsRequired(false);

        builder.HasOne(s => s.Company)
            .WithMany(p => p.Sponsors)
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(s => s.PublicAccessToken)
            .IsRequired();

        builder.HasIndex(s => s.PublicAccessToken)
            .IsUnique();

        builder.Property(s => s.NotificationPreference)
            .HasDefaultValue(Entities.Enums.SponsorNotificationPreference.EveryDocument);

        base.Configure(builder);
    }
}