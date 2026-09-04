using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fonbec.Web.DataAccess.Configurations;

internal class RejectedReasonConfiguration : IEntityTypeConfiguration<RejectedReason>
{
    public void Configure(EntityTypeBuilder<RejectedReason> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code)
            .IsRequired()
            .HasMaxLength(Constants.MaxLength.RejectedReason.Code);

        builder.Property(r => r.Description)
            .IsRequired()
            .HasMaxLength(Constants.MaxLength.RejectedReason.Description);

        builder.HasData(
            new RejectedReason { Id = 1, Code = "MissingWrittenDate", Description = "No figura la fecha", AppliesToDocumentType = DocumentType.Letter },
            new RejectedReason { Id = 2, Code = "MissingAddressee", Description = "No figura el destinatario", AppliesToDocumentType = DocumentType.Letter },
            new RejectedReason { Id = 3, Code = "MissingAuthor", Description = "No figura el firmante", AppliesToDocumentType = DocumentType.Letter },
            new RejectedReason { Id = 4, Code = "NotALetter", Description = "No es una carta", AppliesToDocumentType = DocumentType.Letter },
            new RejectedReason { Id = 5, Code = "WrongAddressee", Description = "Destinatario incorrecto", AppliesToDocumentType = DocumentType.Letter },
            new RejectedReason { Id = 6, Code = "WrongSigner", Description = "Firmante incorrecto", AppliesToDocumentType = DocumentType.Letter },
            new RejectedReason { Id = 7, Code = "NotReportCard", Description = "No es boletín o libreta", AppliesToDocumentType = DocumentType.ReportCard },
            new RejectedReason { Id = 8, Code = "WrongStudentName", Description = "Nombre del estudiante incorrecto", AppliesToDocumentType = DocumentType.ReportCard },
            new RejectedReason { Id = 9, Code = "Unreadable", Description = "Ilegible", AppliesToDocumentType = null },
            new RejectedReason { Id = 10, Code = "Other", Description = "Otro", AppliesToDocumentType = null, RequiresNotes = true });
    }
}