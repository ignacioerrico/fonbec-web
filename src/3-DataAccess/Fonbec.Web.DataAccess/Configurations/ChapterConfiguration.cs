using Fonbec.Web.DataAccess.Configurations.Abstract;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities;
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

        builder.Property(ch => ch.Description)
            .IsRequired(false)
            .HasMaxLength(MaxLength.Chapter.Description);

        base.Configure(builder);
    }
}