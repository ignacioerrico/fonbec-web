using Fonbec.Web.DataAccess.DataModels.Users.Input;
using Mapster;

namespace Fonbec.Web.Logic.Models.Users.Input;

public record DisableUserInputModel(
    int UserIdToDisable,
    bool DisableUser,
    int ModifiedByUserId
);

public class DisableUserInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<DisableUserInputModel, DisableUserInputDataModel>()
            .Map(dest => dest.UserIdToDisable, src => src.UserIdToDisable)
            .Map(dest => dest.DisableUser, src => src.DisableUser)
            .Map(dest => dest.ModifiedByUserId, src => src.ModifiedByUserId);
    }
}