using Azure.Communication.Email;
using Azure.Storage.Blobs;
using Fonbec.Web.DataAccess;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Options;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Authorization;
using Fonbec.Web.Logic.Options;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Logic.Util;
using Fonbec.Web.Ui.Account.Communication;
using Fonbec.Web.Ui.Authorization;
using Fonbec.Web.Ui.Options;
using Microsoft.AspNetCore.Authorization;
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

        services.Configure<BlobStorageOptions>(
            configuration.GetSection(BlobStorageOptions.SectionName));

        services.Configure<DocumentQueueOptions>(
            configuration.GetSection(DocumentQueueOptions.SectionName));

        services.Configure<ReviewOptions>(
            configuration.GetSection(ReviewOptions.SectionName));
    }

    public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMudServices();

        services.AddMudExtensions();

        services.AddScoped<IAuthorizationHandler, FonbecPermissionHandler>();

        // Registers a singleton List<PageAccessInfo> by reflecting over Blazor page components with PageMetadataAttribute;
        // obtain it via constructor injection of List<PageAccessInfo>.
        services.AddSingleton(_ => PageAccessDiscovery.DiscoverPages());

        services.AddScoped<IUserClaimsPrincipalFactory<FonbecWebUser>, FonbecUserClaimsPrincipalFactory>(); // Overrides registration done internally by AddIdentityCore

        services.AddScoped<IPasswordGeneratorWrapper, PasswordGeneratorWrapper>();

        services.AddSingleton<IEmailMessageSender, EmailMessageSender>(); // Sends email messages using Azure Communication Services
        services.AddSingleton<IEmailSender, EmailMessageSenderWrapper>();
        services.AddSingleton<IEmailSender<FonbecWebUser>, IdentityEmailSender>(); // Used by Identity UI; leverages IEmailSender

        var communicationServiceConnectionString =
            configuration.GetConnectionString("CommunicationServiceConnectionString");
        services.AddSingleton(_ => new EmailClient(communicationServiceConnectionString));

        var blobStorageConnectionString =
            configuration.GetConnectionString("BlobStorageConnectionString")
            ?? throw new InvalidOperationException("Connection string 'BlobStorageConnectionString' not found.");
        services.AddSingleton(_ => new BlobServiceClient(blobStorageConnectionString));
        services.AddScoped<IBlobStorageService, BlobStorageService>();

        services.AddScoped<IChapterService, ChapterService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ILetterPlanProgressService, LetterPlanProgressService>();
        services.AddScoped<IPlanCompletionService, PlanCompletionService>();
        services.AddScoped<IPlannedDeliveryService, PlannedDeliveryService>();
        services.AddScoped<IStudentService, StudentService>();
        services.AddScoped<IFacilitatorService, FacilitatorService>();
        services.AddScoped<IFacilitatorUploadService, FacilitatorUploadService>();
        services.AddScoped<IManagerUploadService, ManagerUploadService>();
        services.AddScoped<ILetterExemptionService, LetterExemptionService>();
        services.AddScoped<ISponsorService, SponsorService>();
        services.AddScoped<ISponsorshipService, SponsorshipService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentNotificationService, DocumentNotificationService>();
        services.AddScoped<ICandidateNamePickerService, CandidateNamePickerService>();

        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IChapterRepository, ChapterRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ILetterPlanProgressRepository, LetterPlanProgressRepository>();
        services.AddScoped<IPlannedDeliveryRepository, PlannedDeliveryRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IFacilitatorRepository, FacilitatorRepository>();
        services.AddScoped<IManagerUploadRepository, ManagerUploadRepository>();
        services.AddScoped<ILetterExemptionRepository, LetterExemptionRepository>();
        services.AddScoped<ISponsorRepository, SponsorRepository>();
        services.AddScoped<ISponsorshipRepository, SponsorshipRepository>();
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
    }

    public static void RegisterPolicies(IServiceCollection services)
    {
        var allPages = services.BuildServiceProvider().GetService<List<PageAccessInfo>>()
                       ?? throw new NullReferenceException("Could not resolve List<PageAccessInfo>.");

        services.AddAuthorization(options =>
        {
            foreach (var claim in allPages)
            {
                options.AddPolicy(claim.Codename,
                    policy => policy.Requirements.Add(new FonbecPermissionRequirement(claim.Codename)));
            }
        });
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