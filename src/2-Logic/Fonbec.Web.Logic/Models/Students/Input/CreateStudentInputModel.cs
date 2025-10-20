using Fonbec.Web.DataAccess.DataModels.Students.Input;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Students.Input;

public record CreateStudentInputModel(
    int ChapterId,
    string StudentFirstName,
    string StudentLastName,
    string StudentNickName,
    Gender StudentGender,
    string StudentEmail,
    string StudentPhone,
    string StudentNotes,
    DateTime? StudentSecondarySchoolStartYear,
    DateTime? StudentUniversityStartYear,
    int FacilitatorId,
    int CreatedById
);

public class CreateStudentInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateStudentInputModel, CreateStudentInputDataModel>()
            .Map(dest => dest.ChapterId, src => src.ChapterId)
            .Map(dest => dest.StudentFirstName, src => src.StudentFirstName.NormalizeText())
            .Map(dest => dest.StudentLastName, src => src.StudentLastName.NormalizeText())
            .Map(dest => dest.StudentNickName, src => src.StudentNickName.NormalizeText(),
                src => src.StudentNickName.Trim() != string.Empty)
            .Map(dest => dest.StudentGender, src => src.StudentGender)
            .Map(dest => dest.StudentEmail, src => src.StudentEmail,
                src => src.StudentEmail.Trim() != string.Empty)
            .Map(dest => dest.StudentPhoneNumber, src => src.StudentPhone,
                src => src.StudentPhone.Trim() != string.Empty)
            .Map(dest => dest.StudentNotes, src => src.StudentNotes,
                src => src.StudentNotes.Trim() != string.Empty)
            .Map(dest => dest.StudentSecondarySchoolStartYear, src => src.StudentSecondarySchoolStartYear)
            .Map(dest => dest.StudentUniversityStartYear, src => src.StudentUniversityStartYear)
            .Map(dest => dest.FacilitatorId, src => src.FacilitatorId)
            .Map(dest => dest.CreatedById, src => src.CreatedById);
    }
}