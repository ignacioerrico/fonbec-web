using System;
using System.Collections.Generic;
using System.Text;
using Fonbec.Web.DataAccess.DataModels.Students;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Models.Students;
using Mapster;

public class SponsorStudentsListViewModel : AuditableViewModel, IDetectChanges<SponsorStudentsListViewModel>
{
    public int StudentId { get; set; }

    public string StudentFirstName { get; set; } = null!;

    public string StudentLastName { get; set; } = null!;

    public string? StundentNickName { get; set; }

    public Gender StudentGender { get; set; }

    public bool IsStudentActive { get; set; }

    public string StudentCurrentEducationLevel { get; set; } = string.Empty;

    public string StudentEmail { get; set; } = string.Empty;

    public string StudentPhoneNumber { get; set; } = string.Empty;

    public bool IsIdenticalTo(SponsorStudentsListViewModel other) =>
        StudentFirstName == other.StudentFirstName.NormalizeText()
        && StudentLastName == other.StudentLastName.NormalizeText()
        && StundentNickName == other.StundentNickName
        && Notes == other.Notes.NullOrTrimmed();
}

public class SponsorStudentsListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SponsorStudentsDataModel, SponsorStudentsListViewModel>()
            .Map(dest => dest.StudentId, src => src.StudentId)
            .Map(dest => dest.StudentFirstName, src => src.StudentFirstName)
            .Map(dest => dest.StudentLastName, src => src.StudentLastName)
            .Map(dest => dest.StundentNickName, src => src.StundentNickName ?? string.Empty)
            .Map(dest => dest.StudentGender, src => src.StudentGender)
            .Map(dest => dest.IsStudentActive, src => src.IsStudentActive)
            .Map(dest => dest.StudentCurrentEducationLevel, src => src.StudentCurrentEducationLevel.EnumToString())
            .Map(dest => dest.StudentEmail, src => src.StudentEmail ?? string.Empty)
            .Map(dest => dest.StudentPhoneNumber, src => src.StudentPhoneNumber ?? string.Empty);
    }
}

