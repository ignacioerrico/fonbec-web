using Fonbec.Web.DataAccess.Configurations.Abstract;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class PointOfContactConfiguration : AuditableEntityTypeConfiguration<PointOfContact>
{
    public override void Configure(EntityTypeBuilder<PointOfContact> builder)
    {
        builder.HasKey(poc => poc.Id);

        builder.Property(poc => poc.FirstName)
            .IsRequired()
            .HasMaxLength(MaxLength.PointOfContact.FirstName);

        builder.Property(poc => poc.LastName)
            .IsRequired()
            .HasMaxLength(MaxLength.PointOfContact.LastName);

        builder.Property(poc => poc.NickName)
            .HasMaxLength(MaxLength.PointOfContact.NickName);

        builder.Property(poc => poc.Email)
            .HasMaxLength(MaxLength.PointOfContact.Email);

        builder.Property(poc => poc.PhoneNumber)
            .HasMaxLength(MaxLength.PointOfContact.PhoneNumber);

        builder.HasOne(poc => poc.Company)
          .WithMany(c => c.PointsOfContact)
          .HasForeignKey(poc => poc.CompanyId)
          .OnDelete(DeleteBehavior.NoAction);

        base.Configure(builder);
    }
}