using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class ChapterEmailTemplateConfiguration : IEntityTypeConfiguration<ChapterEmailTemplate>
{
    public void Configure(EntityTypeBuilder<ChapterEmailTemplate> builder)
    {
        builder.HasKey(template => template.Id);

        builder.Property(template => template.Subject)
            .IsRequired()
            .HasMaxLength(MaxLength.ChapterEmailTemplate.Subject);

        builder.Property(template => template.Body)
            .IsRequired()
            .HasMaxLength(MaxLength.ChapterEmailTemplate.Body);

        builder.HasIndex(template => new { template.ChapterId, template.Kind })
            .IsUnique();
    }
}