using System.ComponentModel.DataAnnotations;
using Fonbec.Web.DataAccess;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.Ui.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Fonbec.Web.Ui.Configuration;

public static class ConfigureMiddleware
{
    public static async Task ApplyMigrationsAndSeedingAsync(WebApplication app, bool applySeeding)
    {
        // Run async seeding at startup to ensure database is created and seeded
        using var scope = app.Services.CreateScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<FonbecWebDbContext>>();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        // Apply migrations first
        await dbContext.Database.MigrateAsync();

        // Now trigger async seeding
        // (EF Core will call your UseAsyncSeeding delegate)
        if (applySeeding)
        {
            await dbContext.Database.EnsureCreatedAsync();
        }
    }

    /// <summary>
    /// Run seeding at startup to ensure roles and admin user exist
    /// </summary>
    /// <param name="app">The Web application</param>
    /// <returns></returns>
    /// <exception cref="ValidationException"></exception>
    public static async Task SeedRolesAndAdminUserAsync(WebApplication app)
    {
        // EF Core's seeding delegates (UseSeeding, UseAsyncSeeding) run during migrations or context creation, but you cannot resolve UserManager<TUser> (a scoped service) from there.
        // Identity user creation requires UserManager<TUser>, which must be resolved from a DI scope, not the root provider.
        // The correct, safe approach for a Blazor app:
        // 1. Use EF Core seeding delegates for simple data only
        //    - Seed roles directly using the DbContext in UseSeeding and UseAsyncSeeding.
        //    - Do not seed users with UserManager<TUser> in these delegates.
        // 2. Seed users (and roles, if you want) at application startup in Program.cs (this code).
        //    - After building the app, create a DI scope.
        //    - Resolve UserManager<TUser> and RoleManager<TRole> from the scope.
        //    - Seed your admin user and assign roles.

        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FonbecWebUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<FonbecWebRole>>();
        var adminUserOptions = scope.ServiceProvider.GetRequiredService<IOptions<AdminUserOptions>>().Value;

        // Ensure roles exist
        foreach (var roleName in FonbecRole.AllRoles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var newRole = new FonbecWebRole
            {
                Name = roleName,
                NormalizedName = roleName.ToUpper()
            };
            await roleManager.CreateAsync(newRole);
        }

        // Ensure admin user exists
        var username = adminUserOptions.Username;
        var password = adminUserOptions.Password;
        
        if (username is null || password is null)
        {
            throw new ValidationException("Either username or password has not been set as a secret.");
        }

        var adminUser = await userManager.FindByNameAsync(username);

        if (adminUser is not null)
        {
            // Admin user already exists
            return;
        }

        adminUser = new FonbecWebUser
        {
            FirstName = adminUserOptions.FirstName ?? string.Empty,
            LastName = adminUserOptions.LastName ?? string.Empty,
            NickName = adminUserOptions.NickName,
            Gender = adminUserOptions.Gender,
            UserName = username,
            Email = username,
        };

        // Create admin user
        var userCreationResult = await userManager.CreateAsync(adminUser, password);
        if (!userCreationResult.Succeeded)
        {
            Halt("Could not create admin user: ", userCreationResult.Errors);
        }

        // Ensure that the admin user's email is marked as confirmed in the database, so that no manual confirmation via email is required
        var emailConfirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(adminUser);
        var emailConfirmationResult = await userManager.ConfirmEmailAsync(adminUser, emailConfirmationToken);
        if (!emailConfirmationResult.Succeeded)
        {
            Halt("Could not confirm email for Admin user.", emailConfirmationResult.Errors);
        }

        // Admin user cannot be locked out
        var lockoutDisabled = await userManager.SetLockoutEnabledAsync(adminUser, false);
        if (!lockoutDisabled.Succeeded)
        {
            Halt("Could not disable lockout for admin user: ", lockoutDisabled.Errors);
        }

        // Add Admin role
        var roleAssignmentResult = await userManager.AddToRoleAsync(adminUser, FonbecRole.Admin);
        if (!roleAssignmentResult.Succeeded)
        {
            Halt("Could not add Admin role to admin user: ", roleAssignmentResult.Errors);
        }

        // TODO: Add claims
    }

    private static void Halt(string errorMessage, IEnumerable<IdentityError> errors)
    {
        var errorDescriptions = string.Join(", ", errors.Select(e => e.Description));
        var exceptionMessage = $"{errorMessage} {errorDescriptions}";

        throw new Exception(exceptionMessage);
    }
}