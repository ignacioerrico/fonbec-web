using Fonbec.Web.DataAccess.DataModels.Users.Input;
using Fonbec.Web.DataAccess.Entities.Enums;
using Mapster;

namespace Fonbec.Web.Logic.Models.Users.Input;

public record CreateUserInputModel(
    string UserFirstName,
    string UserLastName,
    string UserNickName,
    List<string> UserRoles,
    Gender UserGender,
    string UserEmail,
    string UserPhoneNumber,
    int CreatedById
);

public class CreateUserInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateUserInputModel, CreateUserInputDataModel>()
            .Map(dest => dest.UserFirstName, src => src.UserFirstName)
            .Map(dest => dest.UserLastName, src => src.UserLastName)
            .Map(dest => dest.UserNickName, src => src.UserNickName,
                src => !string.IsNullOrWhiteSpace(src.UserNickName))
            .Map(dest => dest.UserRoles, src => src.UserRoles)
            .Map(dest => dest.UserGender, src => src.UserGender)
            .Map(dest => dest.UserEmail, src => src.UserEmail.Trim().ToLower())
            .Map(dest => dest.UserPhoneNumber, src => src.UserPhoneNumber.Trim(),
                src => !string.IsNullOrWhiteSpace(src.UserPhoneNumber))
            .Map(dest => dest.CreatedById, src => src.CreatedById)
            .Map(dest => dest.GeneratedPassword, src => MapContext.Current!.Parameters["generatedPassword"],
                src => MapContext.Current != null && MapContext.Current.Parameters.ContainsKey("generatedPassword"));
    }
}