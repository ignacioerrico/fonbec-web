using System.Security.Claims;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Users.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Authorization;
using Fonbec.Web.Logic.Constants;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Models.Results;
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
    Task<List<SelectableModel<int>>> GetAllUsersInRoleForSelectionAsync(string role, int? chapterId = null);
    Task<GetUserOutputModel> GetUserAsync(int userId);
    Task<(int userId, List<string> errors)> CreateUserAsync(CreateUserInputModel model);
    Task<bool> UpdateUserAsync(UpdateUserInputModel model);
    Task<List<string>> DisableUserAsync(DisableUserInputModel model);
    Task<IdentityResult> DeleteForeverAsync(int userId);
    Task<IdentityResult> ResetPasswordAsync(int userId, string userEmail);
    string? GetFonbecAuthClaim(ClaimsPrincipal principal);
    Task<string> GetFonbecAuthClaim(int userId);
    Task<string> GetUserClaim(int userId, string claimType);
    Task SetFonbecAuthClaim(int userId, IEnumerable<string> deniedPages);
    Task SetUserClaim(int userId, string claimType, string claimValue);
    string? GetFonbecGrantsClaim(ClaimsPrincipal principal);
    Task<string> GetFonbecGrantsClaim(int userId);
    bool HasPermission(
        string? fonbecAuthClaimValue,
        string userRole,
        string page,
        string? fonbecGrantsClaimValue = null);
    Task<bool> HasDigitalImprovementGrantAsync(int userId);
    Task<CrudResult> SetDigitalImprovementGrantAsync(
        int targetUserId,
        bool granted,
        string actorRole,
        int? actorChapterId);
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

        var grantsByUserId = await userRepository.GetUserClaimsOfTypeAsync(FonbecGrants.ClaimType);

        foreach (var user in allUsers)
        {
            user.UserRole = allUsersDataModel.UsersInRoles
                .First(u => u.UserIdsInRole.Contains(user.UserId))
                .Role;

            user.HasDigitalImprovementGrant = HasPermission(
                null,
                user.UserRole,
                DocumentPermission.DigitalImprovement,
                grantsByUserId.GetValueOrDefault(user.UserId));
        }

        return allUsers;
    }

    public async Task<List<SelectableModel<int>>> GetAllUsersInRoleForSelectionAsync(string role, int? chapterId = null)
    {
        var usersInRole = await userRepository.GetAllUsersInRoleForSelectionAsync(role, chapterId);
        return usersInRole.Select(u => new SelectableModel<int>(u.Key, u.Value)).ToList();
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

    public async Task<IdentityResult> ResetPasswordAsync(int userId, string userEmail)
    {
        var generatedPassword = await passwordGenerator.GeneratePassword();

        var resetPasswordInputDataModel = new ResetPasswordInputDataModel
        { 
            UserId = userId.Adapt<string>(),
            NewPassword = generatedPassword,
        };

        var identityResult = await userRepository.ResetPasswordAsync(resetPasswordInputDataModel);
        if (identityResult.Succeeded)
        {
            // TODO: Use a nice email template
            await emailMessageSender.SendEmailAsync(userEmail,
                "FONBEC | Tu nueva contraseña",
                $"<p>Usuario: {userEmail}</p><p>Contraseña nueva: {generatedPassword}</p>");
        }

        return identityResult;
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

    public async Task SetFonbecAuthClaim(int userId, IEnumerable<string> deniedCodenames)
    {
        var orderedDenials = deniedCodenames
            .Where(cn => !OptInPermissions.IsOptIn(cn))
            .OrderBy(cn => cn)
            .ToList();
        if (orderedDenials.Count == 0)
        {
            await userRepository.RemoveUserClaim(userId.Adapt<string>(), FonbecAuth.ClaimType);
            return;
        }

        var claimValue = string.Join(",", orderedDenials);
        await SetUserClaim(userId, FonbecAuth.ClaimType, claimValue);
    }

    public async Task SetUserClaim(int userId, string claimType, string claimValue)
    {
        var userIdString = userId.Adapt<string>();
        await userRepository.SetUserClaim(userIdString, claimType, claimValue);
    }

    public string? GetFonbecGrantsClaim(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(FonbecGrants.ClaimType);
    }

    public async Task<string> GetFonbecGrantsClaim(int userId)
    {
        return await GetUserClaim(userId, FonbecGrants.ClaimType);
    }

    /// <summary>
    /// Page permissions are stored in "FonbecAuth" as a comma-separated deny-list of page codenames.
    /// Users have access by default to all pages allowed for their role.
    /// Opt-in feature permissions (see <see cref="OptInPermissions"/>) are stored in "FonbecGrants"
    /// as a comma-separated allow-list and are off by default.
    /// </summary>
    public bool HasPermission(
        string? fonbecAuthClaimValue,
        string userRole,
        string page,
        string? fonbecGrantsClaimValue = null)
    {
        var optIn = OptInPermissions.All.FirstOrDefault(p => p.Codename == page);
        if (optIn is not null)
        {
            return optIn.Roles.Contains(userRole)
                   && ParseCodenames(fonbecGrantsClaimValue).Contains(page);
        }

        var pageInfo = allPages.FirstOrDefault(p => p.Codename == page);
        if (pageInfo is null || !pageInfo.Roles.Contains(userRole))
        {
            return false;
        }

        var deniedPages = ParseCodenames(fonbecAuthClaimValue);
        return !deniedPages.Contains(page);
    }

    public async Task<bool> HasDigitalImprovementGrantAsync(int userId)
    {
        var user = await GetUserAsync(userId);
        if (user is null || string.IsNullOrEmpty(user.UserRole))
        {
            return false;
        }

        var grants = await GetFonbecGrantsClaim(userId);
        return HasPermission(null, user.UserRole, DocumentPermission.DigitalImprovement, grants);
    }

    public async Task<CrudResult> SetDigitalImprovementGrantAsync(
        int targetUserId,
        bool granted,
        string actorRole,
        int? actorChapterId)
    {
        var target = await GetUserAsync(targetUserId);
        if (target is null || string.IsNullOrEmpty(target.UserRole))
        {
            return new CrudResult(Errors: [UserMessages.UserNotFound]);
        }

        if (target.UserRole is not (FonbecRole.Reviewer or FonbecRole.Manager))
        {
            return new CrudResult(Errors: [UserMessages.CannotGrantDigitalImprovementToRole]);
        }

        var actorIsAdmin = actorRole == FonbecRole.Admin;
        var actorIsSameChapterManager = actorRole == FonbecRole.Manager
                                        && actorChapterId is not null
                                        && actorChapterId == target.ChapterId;

        if (!actorIsAdmin && !actorIsSameChapterManager)
        {
            return actorRole == FonbecRole.Manager
                ? new CrudResult(Errors: [UserMessages.CannotGrantDigitalImprovementOutsideChapter])
                : new CrudResult(Errors: [UserMessages.NotAuthorizedToManageDigitalImprovement]);
        }

        await SetOptInGrantAsync(targetUserId, DocumentPermission.DigitalImprovement, granted);
        await StripLegacyDigitalImprovementDenialAsync(targetUserId);

        return new CrudResult(1);
    }

    private async Task SetOptInGrantAsync(int userId, string codename, bool granted)
    {
        var grants = ParseCodenames(await GetFonbecGrantsClaim(userId));
        if (granted)
        {
            grants.Add(codename);
        }
        else
        {
            grants.Remove(codename);
        }

        var ordered = grants.OrderBy(cn => cn).ToList();
        if (ordered.Count == 0)
        {
            await userRepository.RemoveUserClaim(userId.Adapt<string>(), FonbecGrants.ClaimType);
            return;
        }

        await SetUserClaim(userId, FonbecGrants.ClaimType, string.Join(",", ordered));
    }

    private async Task StripLegacyDigitalImprovementDenialAsync(int userId)
    {
        var denied = ParseCodenames(await GetFonbecAuthClaim(userId));
        if (!denied.Remove(DocumentPermission.DigitalImprovement))
        {
            return;
        }

        await SetFonbecAuthClaim(userId, denied);
    }

    private static HashSet<string> ParseCodenames(string? claimValue)
    {
        if (string.IsNullOrWhiteSpace(claimValue))
        {
            return [];
        }

        return claimValue.Split(',')
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToHashSet();
    }
}