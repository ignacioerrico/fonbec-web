using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class DocumentQueueItemConfiguration : IEntityTypeConfiguration<DocumentQueueItem>
{
    public void Configure(EntityTypeBuilder<DocumentQueueItem> builder)
    {
        builder.HasKey(q => q.QueueItemId);

        builder.HasIndex(q => q.DocumentId)
            .IsUnique();

        builder.HasOne(q => q.Document)
            .WithOne(d => d.QueueItem)
            .HasForeignKey<DocumentQueueItem>(q => q.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(q => q.ReviewLockedBy)
            .WithMany()
            .HasForeignKey(q => q.ReviewLockedById)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(q => q.Priority)
            .HasDefaultValue(0);

        builder.Property(q => q.DequeueCount)
            .HasDefaultValue(0);
    }
}