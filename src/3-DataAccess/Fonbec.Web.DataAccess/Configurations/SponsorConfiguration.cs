using Fonbec.Web.DataAccess.Configurations.Abstract;
using Fonbec.Web.DataAccess.Entities;
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

        base.Configure(builder);
    }
}