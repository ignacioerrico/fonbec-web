using Azure.Communication.Email;
using Fonbec.Web.DataAccess;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Account.Communication;
using Fonbec.Web.Ui.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
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

    public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMudServices();

        services.AddMudExtensions();

        services.AddSingleton<IEmailSender<FonbecWebUser>, IdentityEmailSender>();
        services.AddSingleton<IEmailSender, EmailSender>();
        services.AddSingleton<IEmailSenderService, EmailSenderService>();

        var communicationServiceConnectionString =
            configuration.GetConnectionString("CommunicationServiceConnectionString");
        services.AddSingleton(_ => new EmailClient(communicationServiceConnectionString));

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