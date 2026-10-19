using System.Security.Claims;
using Fonbec.Web.DataAccess;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Account;
using Fonbec.Web.Ui.Components;
using Fonbec.Web.Ui.Configuration;
using Fonbec.Web.Ui.Constants;
using Google.Apis.Auth.AspNetCore3;
using Mapster;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Blazor Server streams uploaded files over the circuit's SignalR connection, so the
// maximum receive message size must accommodate the largest allowed document plus overhead.
const long KB = 1024;
const long MB = KB * KB;
var maxFileSizeBytes = builder.Configuration.GetValue<long?>("BlobStorage:MaxFileSizeBytes")
                       ?? 10 * MB;
var maxUploadMessageSize = maxFileSizeBytes + 1 * MB;
builder.Services.Configure<Microsoft.AspNetCore.SignalR.HubOptions>(options =>
    options.MaximumReceiveMessageSize = maxUploadMessageSize);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    options.MultipartBodyLengthLimit = maxUploadMessageSize);
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
    options.Limits.MaxRequestBodySize = maxUploadMessageSize);

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<RequestTheme>();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var googleSignInConfigured = !string.IsNullOrWhiteSpace(googleClientId)
                             && !string.IsNullOrWhiteSpace(googleClientSecret);
if (googleSignInConfigured)
{
    builder.Services.AddAuthentication().AddGoogleOpenIdConnect(
        GoogleOpenIdConnectDefaults.AuthenticationScheme,
        "Google",
        googleOptions =>
        {
            googleOptions.ClientId = googleClientId;
            googleOptions.ClientSecret = googleClientSecret;
            // OpenID Connect's default callback is /signin-oidc. The Cloud Console
            // redirect URI for this app is /signin-google.
            googleOptions.CallbackPath = "/signin-google";
            googleOptions.Events ??= new OpenIdConnectEvents();
            googleOptions.Events.OnRemoteFailure =
                ExternalSignInFailure.For(GoogleOpenIdConnectDefaults.AuthenticationScheme);
        });
}

var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
var facebookSignInConfigured = !string.IsNullOrWhiteSpace(facebookAppId)
                               && !string.IsNullOrWhiteSpace(facebookAppSecret);
if (facebookSignInConfigured)
{
    builder.Services.AddAuthentication().AddFacebook(
        FacebookDefaults.AuthenticationScheme,
        "Facebook",
        facebookOptions =>
        {
            facebookOptions.AppId = facebookAppId;
            facebookOptions.AppSecret = facebookAppSecret;
            // CallbackPath defaults to /signin-facebook. FacebookOptions already requests
            // the email scope and the email field. Cancellation is handled here so the
            // login page can show a Spanish message; AccessDeniedPath would skip that.
            facebookOptions.Events.OnRemoteFailure =
                ExternalSignInFailure.For(FacebookDefaults.AuthenticationScheme);
        });
}

// Azure App Service terminates TLS and forwards the request as HTTP. Without these
// headers, Facebook's redirect URI is built with http and the provider rejects it.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var defaultLockoutTimeSpanInMinutes = builder.Configuration.GetValue<int>("Identity:Lockout:DefaultLockoutTimeSpanInMinutes");

builder.Services.AddIdentityCore<FonbecWebUser>(options =>
    {
        // Only unique email addresses
        options.User.RequireUniqueEmail = true;

        // Protect against brute force attacks
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(defaultLockoutTimeSpanInMinutes);
        options.Lockout.MaxFailedAccessAttempts = builder.Configuration.GetValue<int>("Identity:Lockout:MaxFailedAccessAttempts");

        // Password requirements
        options.Password.RequiredLength = builder.Configuration.GetValue<int>("Identity:Password:RequiredLength");
        options.Password.RequireUppercase = builder.Configuration.GetValue<bool>("Identity:Password:RequireUppercase");
        options.Password.RequireLowercase = builder.Configuration.GetValue<bool>("Identity:Password:RequireLowercase");
        options.Password.RequireDigit = builder.Configuration.GetValue<bool>("Identity:Password:RequireDigit");
        options.Password.RequireNonAlphanumeric = builder.Configuration.GetValue<bool>("Identity:Password:RequireNonAlphanumeric");
    })
    .AddRoles<FonbecWebRole>()
    .AddEntityFrameworkStores<FonbecWebDbContext>()
    .AddSignInManager<FonbecSignInManager>()
    .AddErrorDescriber<SpanishIdentityErrorDescriber>()
    .AddDefaultTokenProviders();

ConfigureServices.RegisterOptions(builder.Services, builder.Configuration);

ConfigureServices.RegisterServices(builder.Services, builder.Configuration);

ConfigureServices.RegisterPolicies(builder.Services);

ConfigureServices.RegisterEntityFrameworkCore(builder.Services, builder.Configuration);

// Mapster (each business Model declares its own mapping)
var logicAssembly = System.Reflection.Assembly.Load("Fonbec.Web.Logic");
TypeAdapterConfig.GlobalSettings.Scan(logicAssembly);

var app = builder.Build();

if (googleSignInConfigured)
{
    app.Logger.LogInformation("Google sign-in is enabled.");
}
else if (!string.IsNullOrWhiteSpace(googleClientId) || !string.IsNullOrWhiteSpace(googleClientSecret))
{
    app.Logger.LogWarning("Google sign-in is disabled because ClientId and ClientSecret must both be configured.");
}
else
{
    app.Logger.LogInformation("Google sign-in is disabled because credentials are not configured.");
}

if (facebookSignInConfigured)
{
    app.Logger.LogInformation("Facebook sign-in is enabled.");
}
else if (!string.IsNullOrWhiteSpace(facebookAppId) || !string.IsNullOrWhiteSpace(facebookAppSecret))
{
    app.Logger.LogWarning("Facebook sign-in is disabled because AppId and AppSecret must both be configured.");
}
else
{
    app.Logger.LogInformation("Facebook sign-in is disabled because credentials are not configured.");
}

// Configure the HTTP request pipeline.

await ConfigureMiddleware.ApplyMigrationsAndSeedingAsync(app, applySeeding: false);

await ConfigureMiddleware.SeedRolesAndAdminUserAsync(app);

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.Use(async (context, next) =>
{
    var requestTheme = context.RequestServices.GetRequiredService<RequestTheme>();
    requestTheme.IsDark = ThemeCookie.IsDark(context.Request);
    await next();
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Serves a document page's active blob for reading in the review workspace. The domain download
// check authorizes the requesting user (reviewers/managers), so no content leaks without it.
app.MapGet(NavRoutes.ReviewDocumentPageRouteTemplate,
    async (long documentId, int pageNumber, ClaimsPrincipal user, IDocumentService documentService) =>
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var userId))
        {
            return Results.Unauthorized();
        }

        var blob = await documentService.DownloadDocumentBlobAsync(documentId, pageNumber, userId);
        return blob is null
            ? Results.NotFound()
            : Results.File(blob.Content, blob.MimeType);
    })
    .RequireAuthorization();

// Manager history: the document must belong to a non-deleted student of the manager's chapter.
app.MapGet(NavRoutes.ManagerStudentDocumentPageTemplate,
    async (int studentId, long documentId, int pageNumber, bool? descargar, ClaimsPrincipal user, IDocumentService documentService) =>
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var userId))
        {
            return Results.Unauthorized();
        }

        var blob = await documentService.DownloadManagerStudentDocumentBlobAsync(
            userId, studentId, documentId, pageNumber);
        if (blob is null)
        {
            return Results.NotFound();
        }

        return descargar == true
            ? Results.File(blob.Content, blob.MimeType, fileDownloadName: blob.FileName)
            : Results.File(blob.Content, blob.MimeType);
    })
    .RequireAuthorization();

// Facilitator history: the student must belong to the signed-in facilitator (same as not found).
app.MapGet(NavRoutes.FacilitatorOtherDocumentPageTemplate,
    async (int studentId, long documentId, int pageNumber, bool? descargar, ClaimsPrincipal user, IDocumentService documentService) =>
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var userId))
        {
            return Results.Unauthorized();
        }

        var blob = await documentService.DownloadFacilitatorOtherDocumentBlobAsync(
            userId, studentId, documentId, pageNumber);
        if (blob is null)
        {
            return Results.NotFound();
        }

        return descargar == true
            ? Results.File(blob.Content, blob.MimeType, fileDownloadName: blob.FileName)
            : Results.File(blob.Content, blob.MimeType);
    })
    .RequireAuthorization();

app.MapGet(NavRoutes.ImproveDocumentPageRouteTemplate,
    async (long documentId, int pageNumber, ClaimsPrincipal user, IDocumentService documentService) =>
    {
        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var userId))
        {
            return Results.Unauthorized();
        }

        var blob = await documentService.DownloadOriginalDocumentBlobAsync(documentId, pageNumber, userId);
        return blob is null
            ? Results.NotFound()
            : Results.File(blob.Content, blob.MimeType);
    })
    .RequireAuthorization();

// Anonymous recipient history downloads: token + DocumentShare authorize the stream (no SAS URLs).
app.MapGet(NavRoutes.SponsorHistoryDownloadRouteTemplate,
    async (Guid token, int studentId, long documentId, int pageNumber, IDocumentService documentService) =>
    {
        var blob = await documentService.DownloadSharedDocumentBlobAsync(
            token, studentId, documentId, pageNumber, isCompany: false);
        return blob is null
            ? Results.NotFound()
            : Results.File(blob.Content, blob.MimeType, fileDownloadName: blob.FileName);
    })
    .AllowAnonymous();

app.MapGet(NavRoutes.CompanyHistoryDownloadRouteTemplate,
    async (Guid token, int studentId, long documentId, int pageNumber, IDocumentService documentService) =>
    {
        var blob = await documentService.DownloadSharedDocumentBlobAsync(
            token, studentId, documentId, pageNumber, isCompany: true);
        return blob is null
            ? Results.NotFound()
            : Results.File(blob.Content, blob.MimeType, fileDownloadName: blob.FileName);
    })
    .AllowAnonymous();

app.Run();
