using Fonbec.Web.DataAccess.Configurations.Abstract;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class SendAlsoToConfiguration : AuditableEntityTypeConfiguration<SendAlsoTo>
{
    public override void Configure(EntityTypeBuilder<SendAlsoTo> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.RecipientName)
            .IsRequired()
            .HasMaxLength(MaxLength.SendAlsoTo.Name);

        builder.Property(s => s.RecipientEmail)
            .IsRequired()
            .HasMaxLength(MaxLength.FonbecWebUser.Email);

        builder.Property(s => s.SendAsBcc)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(s => new { s.SponsorId, s.RecipientEmail })
            .IsUnique();

        builder.HasOne(s => s.Sponsor)
            .WithMany(s => s.SendAlsoTos)
            .HasForeignKey(s => s.SponsorId)
            .OnDelete(DeleteBehavior.NoAction);

        base.Configure(builder);
    }
}