using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations.Abstract;

internal abstract class AuditableEntityTypeConfiguration<T> : IEntityTypeConfiguration<T> where T : Auditable
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.HasOne(a => a.CreatedBy)
            .WithMany()
            .HasForeignKey(a => a.CreatedById)
            .IsRequired();

        builder.HasOne(a => a.LastUpdatedBy)
            .WithMany()
            .HasForeignKey(a => a.LastUpdatedById);

        builder.HasOne(a => a.DisabledBy)
            .WithMany()
            .HasForeignKey(a => a.DisabledById);

        builder.HasOne(a => a.ReenabledBy)
            .WithMany()
            .HasForeignKey(a => a.ReenabledById);

        builder.Property(a => a.Notes)
            .IsRequired(false)
            .HasMaxLength(MaxLength.Auditable.Notes);
    }
}