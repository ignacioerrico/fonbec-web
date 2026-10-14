using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Users.Output;
using Fonbec.Web.DataAccess.Entities.Enums;
using Mapster;

namespace Fonbec.Web.Logic.Models.Users.Output;

public class GetUserOutputModel
{
    public int? ChapterId { get; set; }

    public string UserFullName { get; set; } = null!;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? UserNickName { get; set; }

    public Gender Gender { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public bool CanBeLockedOut { get; set; }

    public string UserRole { get; set; } = null!;
    public string UserRoleTranslated { get; set; } = null!;
}

public class GetUserOutputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<GetUserOutputDataModel, GetUserOutputModel>()
            .Map(dest => dest.ChapterId, src => src.ChapterId)
            .Map(dest => dest.UserFullName, src => src.UserFullName)
            .Map(dest => dest.FirstName, src => src.FirstName)
            .Map(dest => dest.LastName, src => src.LastName)
            .Map(dest => dest.UserNickName, src => src.UserNickName)
            .Map(dest => dest.Gender, src => src.Gender)
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.PhoneNumber, src => src.PhoneNumber)
            .Map(dest => dest.Notes, src => src.Notes)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.CanBeLockedOut, src => src.CanBeLockedOut)
            .Map(dest => dest.UserRole, src => src.UserRole)
            .Map(dest => dest.UserRoleTranslated, src => FonbecRole.Translator[src.UserRole]);
    }
}