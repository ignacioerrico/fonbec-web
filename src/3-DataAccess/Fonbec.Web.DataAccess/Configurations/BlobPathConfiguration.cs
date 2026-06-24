using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class BlobPathConfiguration : IEntityTypeConfiguration<BlobPath>
{
    public void Configure(EntityTypeBuilder<BlobPath> builder)
    {
        builder.HasKey(b => b.BlobPathId);

        builder.Property(b => b.StoragePath)
            .IsRequired()
            .HasMaxLength(Constants.MaxLength.BlobPath.StoragePath);

        builder.Property(b => b.MimeType)
            .IsRequired()
            .HasMaxLength(Constants.MaxLength.BlobPath.MimeType);
    }
}