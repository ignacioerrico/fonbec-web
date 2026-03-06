using Fonbec.Web.DataAccess.Configurations.Abstract;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class CompanyConfiguration : AuditableEntityTypeConfiguration<Company>
{
    public override void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.HasKey(company => company.Id);

        builder.Property(company => company.Name)
            .IsRequired()
            .HasMaxLength(MaxLength.Company.Name);

        builder.Property(company => company.Email)
            .HasMaxLength(MaxLength.Company.Email);

        builder.Property(company => company.PhoneNumber)
            .HasMaxLength(MaxLength.Company.PhoneNumber);

        base.Configure(builder);
    }
}