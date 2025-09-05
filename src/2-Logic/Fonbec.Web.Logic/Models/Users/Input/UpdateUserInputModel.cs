using Fonbec.Web.DataAccess.DataModels.Users.Input;
using Fonbec.Web.DataAccess.Entities.Enums;
using Mapster;

namespace Fonbec.Web.Logic.Models.Users.Input;

public record UpdateUserInputModel(
    int UserId,
    string UserFirstName,
    string UserLastName,
    string UserNickName,
    Gender Gender,
    string UserEmail,
    string UserPhoneNumber,
    int UpdatedById
);

public class UpdateUserInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UpdateUserInputModel, UpdateUserInputDataModel>()
            .Map(dest => dest.UserId, src => src.UserId)
            .Map(dest => dest.UserFirstName, src => src.UserFirstName)
            .Map(dest => dest.UserLastName, src => src.UserLastName)
            .Map(dest => dest.UserNickName, src => src.UserNickName,
                src => !string.IsNullOrWhiteSpace(src.UserNickName))
            .Map(dest => dest.Gender, src => src.Gender)
            .Map(dest => dest.UserEmail, src => src.UserEmail)
            .Map(dest => dest.UserPhoneNumber, src => src.UserPhoneNumber,
                src => !string.IsNullOrWhiteSpace(src.UserPhoneNumber))
            .Map(dest => dest.UpdatedById, src => src.UpdatedById);
    }
}