using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Users;
using Fonbec.Web.DataAccess.DataModels.Users.Input;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IUserRepository
{
    Task<FonbecWebUser?> ValidateUniqueEmailAsync(string userEmail);
    Task<FonbecWebUser?> ValidateUniqueFullNameAsync(string firstName, string lastName);
    Task<(bool isPasswordValid, List<string> errors)> ValidatePasswordAsync(string password);
    Task<AllUsersDataModel> GetAllUsersAsync();
    Task<(int userId, List<string> errors)> CreateUserAsync(CreateUserInputDataModel model);
    Task<bool> UpdateUserAsync(UpdateUserInputDataModel model);
}

public class UserRepository(UserManager<FonbecWebUser> userManager, IUserStore<FonbecWebUser> userStore) : IUserRepository
{
    public async Task<FonbecWebUser?> ValidateUniqueEmailAsync(string userEmail)
    {
        var fonbecUser = await userManager.FindByEmailAsync(userEmail);
        return fonbecUser;
    }

    public async Task<FonbecWebUser?> ValidateUniqueFullNameAsync(string firstName, string lastName)
    {
        var fonbecUser = await userManager.Users.FirstOrDefaultAsync(u =>
            u.FirstName == firstName
            || u.LastName == lastName);
        return fonbecUser;
    }

    public async Task<(bool isPasswordValid, List<string> errors)> ValidatePasswordAsync(string password)
    {
        var errors = new List<string>();

        foreach (var passwordValidator in userManager.PasswordValidators)
        {
            var result = await passwordValidator.ValidateAsync(userManager, null!, password);
            if (!result.Succeeded)
            {
                errors.AddRange(result.Errors.Select(e => e.Description));
            }
        }

        var isPasswordValid = errors.Count == 0;

        return (isPasswordValid, errors);
    }

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

    public async Task<(int userId, List<string> errors)> CreateUserAsync(CreateUserInputDataModel model)
    {
        var userId = 0;
        var errors = new List<string>();

        var fonbecUser = new FonbecWebUser
        {
            FirstName = model.UserFirstName,
            LastName = model.UserLastName,
            NickName = model.UserNickName,
            Gender = model.UserGender,
            PhoneNumber = model.UserPhoneNumber,
        };
        await userStore.SetUserNameAsync(fonbecUser, model.UserEmail, CancellationToken.None);
        await ((IUserEmailStore<FonbecWebUser>)userStore).SetEmailAsync(fonbecUser, model.UserEmail, CancellationToken.None);

        // Create user
        var identityResult = await userManager.CreateAsync(fonbecUser, model.GeneratedPassword);

        if (!identityResult.Succeeded)
        {
            errors.AddRange(identityResult.Errors.Select(e => e.Description));
            return (userId, errors);
        }

        // Add roles
        identityResult = await userManager.AddToRolesAsync(fonbecUser, model.UserRoles);

        if (!identityResult.Succeeded)
        {
            errors.AddRange(identityResult.Errors.Select(e => e.Description));
            return (userId, errors);
        }

        // TODO: Add claims

        // Enable user
        identityResult = await userManager.SetLockoutEnabledAsync(fonbecUser, false);

        if (!identityResult.Succeeded)
        {
            errors.AddRange(identityResult.Errors.Select(e => e.Description));
            return (userId, errors);
        }

        // Get user ID
        var userIdString = await userManager.GetUserIdAsync(fonbecUser);

        if (!int.TryParse(userIdString, out userId))
        {
            errors.Add("User ID could not be parsed.");
        }

        return (userId, errors);
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