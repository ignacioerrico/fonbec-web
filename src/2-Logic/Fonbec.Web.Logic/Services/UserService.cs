using Fonbec.Web.DataAccess.DataModels.Users.Input;
using Fonbec.Web.DataAccess.Repositories;
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
    Task<List<UsersListViewModel>> GetAllUsersAsync();
    Task<List<SelectableModel<int>>> GetAllUsersInRoleForSelectionAsync(string role);
    Task<(int userId, List<string> errors)> CreateUserAsync(CreateUserInputModel model);
    Task<bool> UpdateUserAsync(UpdateUserInputModel model);
    Task<List<string>> DisableUserAsync(int userId, bool disable);
    Task<IdentityResult> DeleteForeverAsync(int userId);
}

public class UserService(
    IUserRepository userRepository,
    IPasswordGeneratorWrapper passwordGenerator,
    IEmailMessageSender emailMessageSender)
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
        var normalizedFirstName = firstName;
        var normalizedLastName = lastName;
        var fonbecUser = await userRepository.ValidateUniqueFullNameAsync(normalizedFirstName, normalizedLastName);

        // Full name is unique if no user is found with the given full name
        var isFullNameUnique = fonbecUser is null;
        return new ValidateUniqueFullNameOutputModel(isFullNameUnique);
    }

    public async Task<List<UsersListViewModel>> GetAllUsersAsync()
    {
        var allUsersDataModel = await userRepository.GetAllUsersAsync();
        var allUsers = allUsersDataModel.Users.Adapt<List<UsersListViewModel>>();

        foreach (var user in allUsers)
        {
            user.Roles = allUsersDataModel.UsersInRoles
                .Where(usersInRole => usersInRole.UserIdsInRole.Contains(user.UserId))
                .Select(usersInRole => usersInRole.Role)
                .ToHashSet();
        }

        return allUsers;
    }

    public async Task<List<SelectableModel<int>>> GetAllUsersInRoleForSelectionAsync(string role)
    {
        var usersInRole = await userRepository.GetAllUsersInRoleForSelectionAsync(role);
        return usersInRole.Select(u => new SelectableModel<int>(u.Key, u.Value)).ToList();
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

    public async Task<List<string>> DisableUserAsync(int userId, bool disable)
    {
        var userIdString = userId.Adapt<string>();
        return await userRepository.DisableUserAsync(userIdString, disable);
    }

    public async Task<IdentityResult> DeleteForeverAsync(int userId)
    {
        var userIdString = userId.Adapt<string>();
        return await userRepository.DeleteForeverAsync(userIdString);
    }
}