using Fonbec.Web.DataAccess.Configurations.Abstract;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class ChapterConfiguration : AuditableEntityTypeConfiguration<Chapter>
{
    public override void Configure(EntityTypeBuilder<Chapter> builder)
    {
        builder.HasKey(ch => ch.Id);

        builder.Property(ch => ch.Name)
            .IsRequired()
            .HasMaxLength(MaxLength.Chapter.Name);

        builder.Property(ch => ch.DocumentRejectionBehavior)
            .HasDefaultValue(DocumentRejectionBehavior.ReturnToFacilitator);

        builder.Property(ch => ch.ShareApprovedReportCardsWithSponsors)
            .HasDefaultValue(true);

        builder.Property(ch => ch.EmailSubjectPrefix)
            .IsRequired()
            .HasMaxLength(MaxLength.Chapter.EmailSubjectPrefix)
            .HasDefaultValue("FONBEC |");

        builder.HasMany(ch => ch.EmailTemplates)
            .WithOne(template => template.Chapter)
            .HasForeignKey(template => template.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);

        base.Configure(builder);
    }
}