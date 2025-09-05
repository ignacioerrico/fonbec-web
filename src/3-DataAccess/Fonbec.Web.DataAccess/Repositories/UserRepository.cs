using Fonbec.Web.DataAccess.DataModels.Users;
using Fonbec.Web.DataAccess.DataModels.Users.Input;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IUserRepository
{
    Task<List<AllUsersDataModel>> GetAllUsersAsync();
    Task<IdentityResult> UpdateUserAsync(UpdateUserInputDataModel model);
}

public class UserRepository(UserManager<FonbecWebUser> userManager) : IUserRepository
{
    public async Task<List<AllUsersDataModel>> GetAllUsersAsync()
    {
        var allUsers = await userManager.Users
            .Select(u =>
                new AllUsersDataModel
                {
                    UserId = u.Id,
                    UserFirstName = u.FirstName,
                    UserLastName = u.LastName,
                    UserNickName = u.NickName,
                    UserGender = u.Gender,
                    UserEmail = u.Email,
                    UserPhoneNumber = u.PhoneNumber,
                    IsUserLockedOut = u.LockoutEnabled,
                    UserLockOutEndsOnUtc = u.LockoutEnd,
                })
            .ToListAsync();

        return allUsers;
    }

    public async Task<IdentityResult> UpdateUserAsync(UpdateUserInputDataModel model)
    {
        var fonbecUserDb = await userManager.FindByIdAsync(model.UserId);
        if (fonbecUserDb is null)
        {
            return IdentityResult.Failed();
        }

        fonbecUserDb.FirstName = model.UserFirstName;
        fonbecUserDb.LastName = model.UserLastName;
        fonbecUserDb.NickName = model.UserNickName;
        fonbecUserDb.Gender = model.Gender;
        fonbecUserDb.Email = model.UserEmail;
        fonbecUserDb.PhoneNumber = model.UserPhoneNumber;

        var identityResult = await userManager.UpdateAsync(fonbecUserDb);
        return identityResult;
    }
}