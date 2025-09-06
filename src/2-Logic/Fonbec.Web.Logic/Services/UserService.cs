using Fonbec.Web.DataAccess.DataModels.Users.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Users;
using Fonbec.Web.Logic.Models.Users.Input;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface IUserService
{
    Task<List<AllUsersViewModel>> GetAllUsersAsync();
    Task<bool> UpdateUserAsync(UpdateUserInputModel model);
}

public class UserService(IUserRepository userRepository) : IUserService
{
    public async Task<List<AllUsersViewModel>> GetAllUsersAsync()
    {
        var allUsersDataModel = await userRepository.GetAllUsersAsync();
        var allUsers = allUsersDataModel.Users.Adapt<List<AllUsersViewModel>>();

        foreach (var user in allUsers)
        {
            user.Roles = allUsersDataModel.UsersInRoles
                .Where(usersInRole => usersInRole.UserIdsInRole.Contains(user.UserId))
                .Select(usersInRole => usersInRole.Role);
        }

        return allUsers;
    }

    public async Task<bool> UpdateUserAsync(UpdateUserInputModel model)
    {
        var updateUserInputDataModel = model.Adapt<UpdateUserInputDataModel>();
        return await userRepository.UpdateUserAsync(updateUserInputDataModel);
    }
}