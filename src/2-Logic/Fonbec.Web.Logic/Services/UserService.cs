using System.Security.Claims;
using Fonbec.Web.DataAccess.DataModels.Users.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Authorization;
using Fonbec.Web.Logic.Constants;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Models.Users;
using Fonbec.Web.Logic.Models.Users.Input;
using Fonbec.Web.Logic.Models.Users.Output;
using Fonbec.Web.Logic.Util;
using Mapster;
using Microsoft.AspNetCore.Identity;

namespace Fonbec.Web.Logic.Services;

public interface IUserService
{
    Task<ValidateUniqueEmailOutputModel> ValidateUniqueEmailAsync(string userEmail);
    Task<ValidateUniqueFullNameOutputModel> ValidateUniqueFullNameAsync(string firstName, string lastName);
    Task<List<UsersListViewModel>> GetAllUsersAsync(int? chapterId);
    Task<List<SelectableModel<int>>> GetAllUsersInRoleForSelectionAsync(string role);
    Task<GetUserOutputModel> GetUserAsync(int userId);
    Task<(int userId, List<string> errors)> CreateUserAsync(CreateUserInputModel model);
    Task<bool> UpdateUserAsync(UpdateUserInputModel model);
    Task<List<string>> DisableUserAsync(DisableUserInputModel model);
    Task<IdentityResult> DeleteForeverAsync(int userId);
    string? GetFonbecAuthClaim(ClaimsPrincipal principal);
    Task<string> GetFonbecAuthClaim(int userId);
    Task<string> GetUserClaim(int userId, string claimType);
    Task SetFonbecAuthClaim(int userId, IEnumerable<string> pages);
    Task SetUserClaim(int userId, string claimType, string claimValue);
    bool HasPermission(string fonbecAuthValue, string page);
}

public class UserService(
    IUserRepository userRepository,
    IPasswordGeneratorWrapper passwordGenerator,
    IEmailMessageSender emailMessageSender,
    List<PageAccessInfo> allPages)
    : IUserService
{
    public async Task<ValidateUniqueEmailOutputModel> ValidateUniqueEmailAsync(string userEmail)
    {
        var normalizedEmail = userEmail.Trim().ToLower();
        var fonbecUser = await userRepository.ValidateUniqueEmailAsync(normalizedEmail);

        // Email is unique if no user is found with the given email
        var isEmailUnique = fonbecUser is null;
        return new ValidateUniqueEmailOutputModel(isEmailUnique, fonbecUser?.FullName());
    }

    public async Task<ValidateUniqueFullNameOutputModel> ValidateUniqueFullNameAsync(string firstName, string lastName)
    {
        var normalizedFirstName = firstName.NormalizeText();
        var normalizedLastName = lastName.NormalizeText();
        var fonbecUser = await userRepository.ValidateUniqueFullNameAsync(normalizedFirstName, normalizedLastName);

        // Full name is unique if no user is found with the given full name
        var isFullNameUnique = fonbecUser is null;
        return new ValidateUniqueFullNameOutputModel(isFullNameUnique);
    }

    public async Task<List<UsersListViewModel>> GetAllUsersAsync(int? chapterId)
    {
        var allUsersDataModel = await userRepository.GetAllUsersAsync(chapterId);
        var allUsers = allUsersDataModel.Users.Adapt<List<UsersListViewModel>>();

        foreach (var user in allUsers)
        {
            user.UserRole = allUsersDataModel.UsersInRoles
                .First(u => u.UserIdsInRole.Contains(user.UserId))
                .Role;
        }

        return allUsers;
    }

    public async Task<List<SelectableModel<int>>> GetAllUsersInRoleForSelectionAsync(string role)
    {
        var usersInRole = await userRepository.GetAllUsersInRoleForSelectionAsync(role);
        return usersInRole.Select(u => new SelectableModel<int>(u.Key, u.Value, u.Value)).ToList();
    }

    public async Task<GetUserOutputModel> GetUserAsync(int userId)
    {
        var getUserOutputDataModel = await userRepository.GetUserAsync(userId);
        return getUserOutputDataModel.Adapt<GetUserOutputModel>();
    }

    public async Task<(int userId, List<string> errors)> CreateUserAsync(CreateUserInputModel model)
    {
        var generatedPassword = await passwordGenerator.GeneratePassword();
        var createUserInputDataModel = model
            .BuildAdapter()
            .AddParameters("generatedPassword", generatedPassword)
            .AdaptToType<CreateUserInputDataModel>();

        var (userId, errors) = await userRepository.CreateUserAsync(createUserInputDataModel);

        var pages = allPages.Where(p => p.Roles.Contains(model.UserRole))
            .Select(p => p.Codename);

        await SetFonbecAuthClaim(userId, pages);

        if (userId > 0 && errors.Count == 0)
        {
            // TODO: Use a nice email template
            await emailMessageSender.SendEmailAsync(model.UserEmail,
                "FONBEC | Tu nueva cuenta",
                $"<p>Usuario: {model.UserEmail}</p><p>Contraseña: {generatedPassword}</p>");
        }

        return (userId, errors);
    }

    public async Task<bool> UpdateUserAsync(UpdateUserInputModel model)
    {
        var updateUserInputDataModel = model.Adapt<UpdateUserInputDataModel>();
        return await userRepository.UpdateUserAsync(updateUserInputDataModel);
    }

    public async Task<List<string>> DisableUserAsync(DisableUserInputModel model)
    {
        var disableUserInputDataModel = model.Adapt<DisableUserInputDataModel>();
        return await userRepository.DisableUserAsync(disableUserInputDataModel);
    }

    public async Task<IdentityResult> DeleteForeverAsync(int userId)
    {
        var userIdString = userId.Adapt<string>();
        return await userRepository.DeleteForeverAsync(userIdString);
    }

    public string? GetFonbecAuthClaim(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(FonbecAuth.ClaimType);
    }

    public async Task<string> GetFonbecAuthClaim(int userId)
    {
        return await GetUserClaim(userId, FonbecAuth.ClaimType);
    }

    public async Task<string> GetUserClaim(int userId, string claimType)
    {
        var userIdString = userId.Adapt<string>();
        var userClaim = await userRepository.GetUserClaim(userIdString, claimType);
        return userClaim ?? string.Empty;
    }

    public async Task SetFonbecAuthClaim(int userId, IEnumerable<string> pages)
    {
        var orderedPages = pages.OrderBy(page => page);
        var claimValue = string.Join(",", orderedPages);
        await SetUserClaim(userId, FonbecAuth.ClaimType, claimValue);
    }

    public async Task SetUserClaim(int userId, string claimType, string claimValue)
    {
        var userIdString = userId.Adapt<string>();
        await userRepository.SetUserClaim(userIdString, claimType, claimValue);
    }

    /// <summary>
    /// All permissions are stored in a single claim, "FonbecAuth".
    /// It contains a comma-separated list of page names the user has access to.
    /// </summary>
    /// <param name="fonbecAuthValue">The value of the FonbecAuth claim</param>
    /// <param name="page">The page to check access to</param>
    /// <returns>True if the page is contained in the claim; otherwise, false</returns>
    public bool HasPermission(string fonbecAuthValue, string page)
        => fonbecAuthValue.Split(',')
            .Select(p => p.Trim())
            .Contains(page);
}