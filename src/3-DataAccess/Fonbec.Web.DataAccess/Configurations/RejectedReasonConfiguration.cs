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
            new RejectedReason { Id = 1, Code = "NotALetter", Description = "No es una carta", AppliesToDocumentType = DocumentType.Letter },
            new RejectedReason { Id = 2, Code = "WrongAddressee", Description = "Destinatario incorrecto", AppliesToDocumentType = DocumentType.Letter },
            new RejectedReason { Id = 3, Code = "WrongSigner", Description = "Firma incorrecta", AppliesToDocumentType = DocumentType.Letter },
            new RejectedReason { Id = 4, Code = "Illegible", Description = "Ilegible", AppliesToDocumentType = DocumentType.Letter },
            new RejectedReason { Id = 5, Code = "InappropriateContent", Description = "Contenido inapropiado", AppliesToDocumentType = DocumentType.Letter },
            new RejectedReason { Id = 6, Code = "WrongDate", Description = "Fecha incorrecta", AppliesToDocumentType = DocumentType.Letter },
            new RejectedReason { Id = 7, Code = "NotReportCard", Description = "No es boletín o libreta", AppliesToDocumentType = DocumentType.ReportCard },
            new RejectedReason { Id = 8, Code = "WrongStudentName", Description = "Nombre del estudiante incorrecto", AppliesToDocumentType = DocumentType.ReportCard },
            new RejectedReason { Id = 9, Code = "Unreadable", Description = "No legible", AppliesToDocumentType = DocumentType.Other },
            new RejectedReason { Id = 10, Code = "WrongDocument", Description = "Documento incorrecto", AppliesToDocumentType = DocumentType.Other },
            new RejectedReason { Id = 11, Code = "Other", Description = "Otro", AppliesToDocumentType = null, RequiresNotes = true });
    }
}