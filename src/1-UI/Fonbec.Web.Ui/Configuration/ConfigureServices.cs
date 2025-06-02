using System.ComponentModel.DataAnnotations;
using System.Text;
using Fonbec.Web.DataAccess;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.Ui.Account;
using Fonbec.Web.Ui.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using MudBlazor.Services;
using MudExtensions.Services;

namespace Fonbec.Web.Ui.Configuration;

public static class ConfigureServices
{
    public static void RegisterOptions(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AdminUserOptions>(
            configuration.GetSection(AdminUserOptions.SectionName));
    }

    public static void RegisterServices(IServiceCollection services)
    {
        services.AddMudServices();

        services.AddMudExtensions();

        services.AddSingleton<IEmailSender<FonbecWebUser>, IdentityNoOpEmailSender>();
    }

    public static void RegisterEntityFrameworkCore(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("FonbecWebDbContextConnection") ??
                               throw new InvalidOperationException("Connection string 'FonbecWebDbContextConnection' not found.");

        services.AddDbContext<FonbecWebDbContext>(options =>
            options.UseSqlServer(connectionString)
                .UseSeeding((context, _) =>
                {
                    foreach (var role in FonbecRole.AllRoles)
                    {
                        var roleDb = context.Set<IdentityRole>().FirstOrDefault(ir => ir.Name == role);
                        if (roleDb is not null)
                        {
                            continue;
                        }

                        var identityRole = new IdentityRole
                        {
                            Name = role,
                            NormalizedName = role.ToUpper()
                        };
                        context.Set<IdentityRole>().Add(identityRole);
                        context.SaveChanges();
                    }

                    var adminUserOptions = context.GetService<IOptions<AdminUserOptions>>().Value
                                           ?? throw new NullReferenceException("AdminUserOptions could not be instantiated.");

                    // Set in user secrets.
                    var username = adminUserOptions.Username;
                    var password = adminUserOptions.Password;

                    if (username is null || password is null)
                    {
                        throw new ValidationException("Either username or password has not been set as a secret.");
                    }

                    var userManager = context.GetService<UserManager<FonbecWebUser>>();

                    if (userManager.FindByNameAsync(username).Result is not null)
                    {
                        return;
                    }

                    var adminUser = new FonbecWebUser
                    {
                        UserName = username,
                        Email = username,
                    };

                    var userCreationResult = userManager.CreateAsync(adminUser, password).Result;

                    if (userCreationResult.Succeeded)
                    {
                        var roleAssignmentResult = userManager.AddToRoleAsync(adminUser, FonbecRole.Admin).Result;
                        if (!roleAssignmentResult.Succeeded)
                        {
                            Halt("Could not add Admin role to Admin user.", roleAssignmentResult.Errors);
                        }

                        var removeLockoutResult = userManager.SetLockoutEnabledAsync(adminUser, false).Result;
                        if (!removeLockoutResult.Succeeded)
                        {
                            Halt("Could not remove lockout from Admin user.", removeLockoutResult.Errors);
                        }
                    }
                    else
                    {
                        Halt("Could not create admin user.", userCreationResult.Errors);
                    }
                })
                .UseAsyncSeeding(async (context, _, cancellationToken) =>
                {
                    foreach (var role in FonbecRole.AllRoles)
                    {
                        var roleDb = await context.Set<IdentityRole>().FirstOrDefaultAsync(ir => ir.Name == role, cancellationToken);
                        if (roleDb is not null)
                        {
                            continue;
                        }

                        var identityRole = new IdentityRole
                        {
                            Name = role,
                            NormalizedName = role.ToUpper()
                        };
                        context.Set<IdentityRole>().Add(identityRole);
                        await context.SaveChangesAsync(cancellationToken);
                    }

                    var adminUserOptions = context.GetService<IOptions<AdminUserOptions>>().Value
                       ?? throw new NullReferenceException("AdminUserOptions could not be instantiated.");

                    // Set in user secrets.
                    var username = adminUserOptions.Username;
                    var password = adminUserOptions.Password;

                    if (username is null || password is null)
                    {
                        throw new ValidationException("Either username or password has not been set as a secret.");
                    }

                    var userManager = context.GetService<UserManager<FonbecWebUser>>();

                    if (await userManager.FindByNameAsync(username) is not null)
                    {
                        return;
                    }

                    var adminUser = new FonbecWebUser
                    {
                        UserName = username,
                        Email = username,
                    };

                    var userCreationResult = await userManager.CreateAsync(adminUser, password);

                    if (userCreationResult.Succeeded)
                    {
                        var roleAssignmentResult = await userManager.AddToRoleAsync(adminUser, FonbecRole.Admin);
                        if (!roleAssignmentResult.Succeeded)
                        {
                            Halt("Could not add Admin role to Admin user.", roleAssignmentResult.Errors);
                        }

                        var removeLockoutResult = await userManager.SetLockoutEnabledAsync(adminUser, false);
                        if (!removeLockoutResult.Succeeded)
                        {
                            Halt("Could not remove lockout from Admin user.", removeLockoutResult.Errors);
                        }
                    }
                    else
                    {
                        Halt("Could not create admin user.", userCreationResult.Errors);
                    }
                })
        );
        
        services.AddDatabaseDeveloperPageExceptionFilter();
    }

    private static void Halt(string errorMessage, IEnumerable<IdentityError> errors)
    {
        var sb = new StringBuilder(errorMessage);
        foreach (var error in errors)
        {
            sb.AppendLine(error.Description);
        }

        throw new ValidationException(sb.ToString());
    }
}