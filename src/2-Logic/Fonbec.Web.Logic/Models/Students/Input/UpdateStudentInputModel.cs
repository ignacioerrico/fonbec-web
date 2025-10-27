using Fonbec.Web.DataAccess.DataModels.Students.Input;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Students.Input;

public record UpdateStudentInputModel(
    int StudentId,
    string StudentFirstName,
    string StudentLastName,
    string StudentNickName,
    string StudentEmail,
    string StudentPhone,
    string StudentNotes,
    DateTime? StudentSecondarySchoolStartYear,
    DateTime? StudentUniversityStartYear,
    int FacilitatorId
);

public class UpdateStudentInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UpdateStudentInputModel, UpdateStudentInputDataModel>()
            .Map(dest => dest.StudentId, src => src.StudentId)
            .Map(dest => dest.StudentFirstName, src => src.StudentFirstName.MustBeNonEmpty().NormalizeText())
            .Map(dest => dest.StudentLastName, src => src.StudentLastName.MustBeNonEmpty().NormalizeText())
            .Map(dest => dest.StudentNickName, src => src.StudentNickName.NormalizeText(),
                src => !string.IsNullOrWhiteSpace(src.StudentNickName))
            .Map(dest => dest.StudentEmail, src => src.StudentEmail.Trim().ToLower(),
                src => !string.IsNullOrWhiteSpace(src.StudentEmail))
            .Map(dest => dest.StudentPhoneNumber, src => src.StudentPhone.NullOrTrimmed())
            .Map(dest => dest.StudentNotes, src => src.StudentNotes.NullOrTrimmed())
            .Map(dest => dest.StudentSecondarySchoolStartYear, src => src.StudentSecondarySchoolStartYear)
            .Map(dest => dest.StudentUniversityStartYear, src => src.StudentUniversityStartYear)
            .Map(dest => dest.FacilitatorId, src => src.FacilitatorId);
    }
}