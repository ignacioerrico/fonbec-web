using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class UserRepositorySelectionTests
{
    [Fact]
    public async Task GetAllUsersInRoleForSelectionAsync_WithChapter_ReturnsOnlyUsersFromThatChapter()
    {
        var services = new ServiceCollection();
        services.AddDbContext<FonbecWebDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<FonbecWebUser>()
            .AddRoles<FonbecWebRole>()
            .AddEntityFrameworkStores<FonbecWebDbContext>();

        using var provider = services.BuildServiceProvider();
        var userManager = provider.GetRequiredService<UserManager<FonbecWebUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<FonbecWebRole>>();
        var userStore = provider.GetRequiredService<IUserStore<FonbecWebUser>>();
        var repository = new UserRepository(userManager, userStore);

        (await roleManager.CreateAsync(new FonbecWebRole { Name = FonbecRole.Uploader }))
            .Succeeded.Should().BeTrue();
        var chapterOneFacilitator = await CreateFacilitatorAsync(
            userManager, "chapter-one@test.com", chapterId: 1);
        await CreateFacilitatorAsync(userManager, "chapter-two@test.com", chapterId: 2);

        var result = (await repository.GetAllUsersInRoleForSelectionAsync(
            FonbecRole.Uploader, chapterId: 1)).ToList();

        result.Should().ContainSingle()
            .Which.Key.Should().Be(chapterOneFacilitator.Id);
    }

    private static async Task<FonbecWebUser> CreateFacilitatorAsync(
        UserManager<FonbecWebUser> userManager,
        string email,
        int chapterId)
    {
        var user = new FonbecWebUser
        {
            UserName = email,
            Email = email,
            FirstName = email,
            LastName = "Test",
            ChapterId = chapterId,
        };

        (await userManager.CreateAsync(user)).Succeeded.Should().BeTrue();
        (await userManager.AddToRoleAsync(user, FonbecRole.Uploader)).Succeeded.Should().BeTrue();
        return user;
    }
}
