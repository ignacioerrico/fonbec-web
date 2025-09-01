using Fonbec.Web.DataAccess;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Account;
using Fonbec.Web.Ui.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        services.AddScoped<IChaptersListService, ChaptersListService>();

        services.AddScoped<IChapterRepository, ChapterRepository>();
    }

    public static void RegisterEntityFrameworkCore(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextFactory<FonbecWebDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("FonbecWebDbContextConnection") ??
                                       throw new InvalidOperationException("Connection string 'FonbecWebDbContextConnection' not found.");

                options.UseSqlServer(connectionString);
            }
        );

        services.AddDatabaseDeveloperPageExceptionFilter();
    }
}