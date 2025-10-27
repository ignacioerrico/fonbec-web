using Fonbec.Web.DataAccess.DataModels.Users.Input;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Users.Input;

public record CreateUserInputModel(
    int UserChapterId,
    string UserFirstName,
    string UserLastName,
    string UserNickName,
    Gender UserGender,
    string UserEmail,
    string UserPhoneNumber,
    string UserNotes,
    string UserRole,
    int CreatedById
);

public class CreateUserInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateUserInputModel, CreateUserInputDataModel>()
            .Map(dest => dest.UserChapterId, src => src.UserChapterId)
            .Map(dest => dest.UserFirstName, src => src.UserFirstName.MustBeNonEmpty().NormalizeText())
            .Map(dest => dest.UserLastName, src => src.UserLastName.MustBeNonEmpty().NormalizeText())
            .Map(dest => dest.UserNickName, src => src.UserNickName.NormalizeText(),
                src => !string.IsNullOrWhiteSpace(src.UserNickName))
            .Map(dest => dest.UserGender, src => src.UserGender)
            .Map(dest => dest.UserEmail, src => src.UserEmail.Trim().ToLower())
            .Map(dest => dest.UserPhoneNumber, src => src.UserPhoneNumber.NullOrTrimmed())
            .Map(dest => dest.UserNotes, src => src.UserNotes.NullOrTrimmed())
            .Map(dest => dest.UserRole, src => src.UserRole)
            .Map(dest => dest.CreatedById, src => src.CreatedById)
            .Map(dest => dest.GeneratedPassword, src => MapContext.Current!.Parameters["generatedPassword"],
                src => MapContext.Current != null && MapContext.Current.Parameters.ContainsKey("generatedPassword"));
    }
}