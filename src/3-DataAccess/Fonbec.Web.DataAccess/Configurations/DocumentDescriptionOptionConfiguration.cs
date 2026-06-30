using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class DocumentDescriptionOptionConfiguration : IEntityTypeConfiguration<DocumentDescriptionOption>
{
    public void Configure(EntityTypeBuilder<DocumentDescriptionOption> builder)
    {
        builder.HasKey(o => o.DocumentDescriptionOptionId);

        builder.Property(o => o.Text)
            .IsRequired()
            .HasMaxLength(Constants.MaxLength.DocumentDescriptionOption.Text);

        builder.HasOne(o => o.Chapter)
            .WithMany()
            .HasForeignKey(o => o.ChapterId)
            .OnDelete(DeleteBehavior.NoAction);

        // Explicit null filter so the global defaults (ChapterId IS NULL) are also covered by the
        // uniqueness constraint (SQL Server's default would otherwise exclude NULL chapters).
        builder.HasIndex(o => new { o.ChapterId, o.DocumentType, o.Text })
            .IsUnique()
            .HasFilter(null);

        builder.HasData(
            // Report card / transcript defaults (global).
            new DocumentDescriptionOption { DocumentDescriptionOptionId = 1, ChapterId = null, DocumentType = DocumentType.ReportCard, Text = "Boletín 1º trimestre", SortOrder = 1, IsActive = true },
            new DocumentDescriptionOption { DocumentDescriptionOptionId = 2, ChapterId = null, DocumentType = DocumentType.ReportCard, Text = "Boletín 2º trimestre", SortOrder = 2, IsActive = true },
            new DocumentDescriptionOption { DocumentDescriptionOptionId = 3, ChapterId = null, DocumentType = DocumentType.ReportCard, Text = "Boletín 3º trimestre", SortOrder = 3, IsActive = true },
            new DocumentDescriptionOption { DocumentDescriptionOptionId = 4, ChapterId = null, DocumentType = DocumentType.ReportCard, Text = "Boletín 4º trimestre", SortOrder = 4, IsActive = true },
            new DocumentDescriptionOption { DocumentDescriptionOptionId = 5, ChapterId = null, DocumentType = DocumentType.ReportCard, Text = "Libreta universitaria", SortOrder = 5, IsActive = true },
            // Other document defaults (global).
            new DocumentDescriptionOption { DocumentDescriptionOptionId = 6, ChapterId = null, DocumentType = DocumentType.Other, Text = "Certificado de alumno regular", SortOrder = 1, IsActive = true },
            new DocumentDescriptionOption { DocumentDescriptionOptionId = 7, ChapterId = null, DocumentType = DocumentType.Other, Text = "Constancia", SortOrder = 2, IsActive = true },
            new DocumentDescriptionOption { DocumentDescriptionOptionId = 8, ChapterId = null, DocumentType = DocumentType.Other, Text = "Foto", SortOrder = 3, IsActive = true },
            new DocumentDescriptionOption { DocumentDescriptionOptionId = 9, ChapterId = null, DocumentType = DocumentType.Other, Text = "Otro documento", SortOrder = 4, IsActive = true });
    }
}
