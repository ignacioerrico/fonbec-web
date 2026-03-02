using Fonbec.Web.DataAccess.Configurations.Abstract;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class SponsorshipConfiguration : AuditableEntityTypeConfiguration<Sponsorship>
{
    public override void Configure(EntityTypeBuilder<Sponsorship> builder)
    {
        builder.HasKey(s => s.SponsorshipId);

        // N-M relation
        builder.HasOne(s => s.Student)
            .WithMany(s => s.Sponsorships)
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(s => s.Sponsor)
            .WithMany(s => s.Sponsorships)
            .HasForeignKey(s => s.SponsorId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(s => s.StartDate)
            .IsRequired(true);
        builder.Property(s => s.EndDate)
            .IsRequired(false);

        base.Configure(builder);
    }
}
