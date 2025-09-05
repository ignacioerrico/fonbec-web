using Fonbec.Web.DataAccess.DataModels.Users;
using Fonbec.Web.DataAccess.Entities.Enums;
using Mapster;

namespace Fonbec.Web.Logic.Models.Users;

public class AllUsersViewModel
{
    public int UserId { get; set; }

    public string UserFirstName { get; set; } = null!;

    public string UserLastName { get; set; } = null!;

    public string UserNickName { get; set; } = null!;

    public Gender UserGender { get; set; }

    public string UserEmail { get; set; } = null!;

    public string UserPhoneNumber { get; set; } = null!;

    public bool IsUserActive { get; set; }
}

public class AllUsersViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AllUsersDataModel, AllUsersViewModel>()
            .Map(dest => dest.UserId, src => src.UserId)
            .Map(dest => dest.UserFirstName, src => src.UserFirstName)
            .Map(dest => dest.UserLastName, src => src.UserLastName)
            .Map(dest => dest.UserNickName, src => src.UserNickName, src => src.UserNickName != null)
            .Map(dest => dest.UserNickName, src => string.Empty, src => src.UserNickName == null)
            .Map(dest => dest.UserGender, src => src.UserGender)
            .Map(dest => dest.UserEmail, src => src.UserEmail, src => src.UserEmail != null)
            .Map(dest => dest.UserEmail, src => string.Empty, src => src.UserEmail == null)
            .Map(dest => dest.UserPhoneNumber, src => src.UserPhoneNumber, src => src.UserPhoneNumber != null)
            .Map(dest => dest.UserPhoneNumber, src => string.Empty, src => src.UserPhoneNumber == null)
            .Map(dest => dest.IsUserActive, src =>
                    !src.IsUserLockedOut
                    && src.UserLockOutEndsOnUtc!.Value < DateTimeOffset.Now,
                src => src.UserLockOutEndsOnUtc != null)
            .Map(dest => dest.IsUserActive, src => !src.IsUserLockedOut,
                src => src.UserLockOutEndsOnUtc == null);
    }
}