using Fonbec.Web.DataAccess.DataModels.Users.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Users;
using Fonbec.Web.Logic.Models.Users.Input;
using Mapster;
using Microsoft.AspNetCore.Identity;

namespace Fonbec.Web.Logic.Services;

public interface IUserService
{
    Task<List<AllUsersViewModel>> GetAllUsersAsync();
    Task<IdentityResult> UpdateUserAsync(UpdateUserInputModel model);
}

public class UserService(IUserRepository userRepository) : IUserService
{
    public async Task<List<AllUsersViewModel>> GetAllUsersAsync()
    {
        var allUsersDataModel = await userRepository.GetAllUsersAsync();
        return allUsersDataModel.Adapt<List<AllUsersViewModel>>();
    }

    public async Task<IdentityResult> UpdateUserAsync(UpdateUserInputModel model)
    {
        var updateUserInputDataModel = model.Adapt<UpdateUserInputDataModel>();
        return await userRepository.UpdateUserAsync(updateUserInputDataModel);
    }
}