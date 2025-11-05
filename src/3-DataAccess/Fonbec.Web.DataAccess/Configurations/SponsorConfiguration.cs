using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fonbec.Web.DataAccess.Configurations.Abstract;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class SponsorConfiguration : UserWithoutAccountConfiguration<Sponsor>
{
    public override void Configure(EntityTypeBuilder<Sponsor> builder)
    {
        builder.Property(s => s.Email)
            .IsRequired(true)
            .HasMaxLength(MaxLength.FonbecWebUser.Email);

        base.Configure(builder);
    }
}
