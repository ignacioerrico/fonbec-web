using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Users;
using Fonbec.Web.DataAccess.DataModels.Users.Input;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IUserRepository
{
    Task<AllUsersDataModel> GetAllUsersAsync();
    Task<bool> UpdateUserAsync(UpdateUserInputDataModel model);
}

public class UserRepository(UserManager<FonbecWebUser> userManager) : IUserRepository
{
    public async Task<AllUsersDataModel> GetAllUsersAsync()
    {
        var result = new AllUsersDataModel();

        var users = await userManager.Users
            .Select(u =>
                new AllUsersUserDataModel
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
            .OrderBy(dm => dm.UserFirstName)
            .ToListAsync();

        result.Users = users;

        foreach (var role in FonbecRole.AllRoles)
        {
            var usersInRole = await userManager.GetUsersInRoleAsync(role);

            var usersInRoles = new AllUsersUsersInRolesDataModel
            {
                Role = role,
                UserIdsInRole = usersInRole.Select(u => u.Id),
            };

            result.UsersInRoles.Add(usersInRoles);
        }

        return result;
    }

    public async Task<bool> UpdateUserAsync(UpdateUserInputDataModel model)
    {
        var fonbecUserDb = await userManager.FindByIdAsync(model.UserId);
        if (fonbecUserDb is null)
        {
            return false;
        }

        var oldEmail = fonbecUserDb.Email;
        var newEmail = model.UserEmail;
        
        if (oldEmail != newEmail)
        {
            // Update both user name and email address
            fonbecUserDb.UserName = newEmail;
            fonbecUserDb.Email = newEmail;
        }

        fonbecUserDb.FirstName = model.UserFirstName;
        fonbecUserDb.LastName = model.UserLastName;
        fonbecUserDb.NickName = model.UserNickName;
        fonbecUserDb.Gender = model.Gender;
        fonbecUserDb.PhoneNumber = model.UserPhoneNumber;

        var identityResult = await userManager.UpdateAsync(fonbecUserDb);
        return identityResult.Succeeded;
    }
}