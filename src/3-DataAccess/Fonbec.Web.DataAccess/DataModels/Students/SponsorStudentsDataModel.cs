using System;
using System.Collections.Generic;
using System.Text;
using Fonbec.Web.DataAccess.Entities.Abstract;
using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Students;

public class SponsorStudentsDataModel(Auditable auditable) : AuditableDataModel(auditable)
{
    public int StudentId { get; set; }

    public string StudentFirstName { get; set; } = null!;

    public string StudentLastName { get; set; } = null!;

    public string? StundentNickName { get; set; }

    public Gender StudentGender { get; set; }

    public bool IsStudentActive { get; set; }

    public EducationLevel StudentCurrentEducationLevel { get; set; }

    public string? StudentEmail { get; set; }

    public string? StudentPhoneNumber { get; set; }
}

