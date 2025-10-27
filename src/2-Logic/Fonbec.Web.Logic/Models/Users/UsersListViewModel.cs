using Fonbec.Web.DataAccess.DataModels.Users;
using Fonbec.Web.DataAccess.Entities.Enums;
using Mapster;

namespace Fonbec.Web.Logic.Models.Users;

public class UsersListViewModel
{
    public int UserId { get; set; }

    public string UserFirstName { get; set; } = null!;

    public string UserLastName { get; set; } = null!;

    public string UserNickName { get; set; } = null!;

    public Gender UserGender { get; set; }

    public string UserEmail { get; set; } = null!;

    public string UserPhoneNumber { get; set; } = null!;

    public string UserNotes { get; set; } = null!;

    public string UserRole { get; set; } = null!;

    public string UserChapterName { get; set; } = null!;

    public bool CanUserBeLockedOut { get; set; }
    
    public bool IsUserActive { get; set; }

    public string? CreatedByFullName { get; set; }
    public DateTime CreatedOnUtc { get; set; }

    public string? LastUpdatedByFullName { get; set; }
    public DateTime? LastUpdatedOnUtc { get; set; }

    public string? DisabledByFullName { get; set; }
    public DateTime? DisabledOnUtc { get; set; }
    public DateTime? DisabledUntilUtc { get; set; }

    public string? ReenabledByFullName { get; set; }
    public DateTime? ReenabledOnUtc { get; set; }
}

public class UsersListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AllUsersUserDataModel, UsersListViewModel>()
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
            .Map(dest => dest.UserNotes, src => src.UserNotes ?? string.Empty)
            .Map(dest => dest.CanUserBeLockedOut, src => src.CanUserBeLockedOut)
            .Map(dest => dest.UserChapterName, src => src.UserChapterName, src => src.UserChapterName != null)
            .Map(dest => dest.UserChapterName, src => "GLOBAL", src => src.UserChapterName == null)
            .Map(dest => dest.IsUserActive, src => !src.CanUserBeLockedOut
                                                   || src.UserLockOutEndsOnUtc == null
                                                   || src.UserLockOutEndsOnUtc.Value < DateTimeOffset.Now)
            .Map(dest => dest.CreatedByFullName, src => src.CreatedByFullName)
            .Map(dest => dest.CreatedOnUtc, src => src.CreatedOnUtc)
            .Map(dest => dest.LastUpdatedByFullName, src => src.LastUpdatedByFullName)
            .Map(dest => dest.LastUpdatedOnUtc, src => src.LastUpdatedOnUtc)
            .Map(dest => dest.DisabledByFullName, src => src.DisabledByFullName)
            .Map(dest => dest.DisabledOnUtc, src => src.DisabledOnUtc)
            .Map(dest => dest.DisabledUntilUtc, src => src.UserLockOutEndsOnUtc!.Value.UtcDateTime,
                src => src.UserLockOutEndsOnUtc != null)
            .Map(dest => dest.ReenabledByFullName, src => src.ReenabledByFullName)
            .Map(dest => dest.ReenabledOnUtc, src => src.ReenabledOnUtc);
    }
}