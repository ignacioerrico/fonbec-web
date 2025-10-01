using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Users.Output;
using Mapster;

namespace Fonbec.Web.Logic.Models.Users.Output;

public class GetUserOutputModel
{
    public string UserFullName { get; set; } = null!;

    public string? UserNickName { get; set; }

    public string UserRoleTranslated { get; set; } = null!;
}

public class GetUserOutputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<GetUserOutputDataModel, GetUserOutputModel>()
            .Map(dest => dest.UserFullName, src => src.UserFullName)
            .Map(dest => dest.UserNickName, src => src.UserNickName)
            .Map(dest => dest.UserRoleTranslated, src => FonbecRole.Translator[src.UserRole]);
    }
}