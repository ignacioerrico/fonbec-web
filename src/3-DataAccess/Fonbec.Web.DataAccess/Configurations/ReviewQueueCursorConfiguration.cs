using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class ReviewQueueCursorConfiguration : IEntityTypeConfiguration<ReviewQueueCursor>
{
    public void Configure(EntityTypeBuilder<ReviewQueueCursor> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.RowVersion)
            .IsRowVersion();
    }
}