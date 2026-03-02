using Fonbec.Web.DataAccess;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.Ui.Account;
using Fonbec.Web.Ui.Components;
using Fonbec.Web.Ui.Configuration;
using Mapster;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

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
    .AddSignInManager()
    .AddDefaultTokenProviders();

ConfigureServices.RegisterOptions(builder.Services, builder.Configuration);

ConfigureServices.RegisterServices(builder.Services, builder.Configuration);

ConfigureServices.RegisterPolicies(builder.Services);

ConfigureServices.RegisterEntityFrameworkCore(builder.Services, builder.Configuration);

// Mapster (each business Model declares its own mapping)
var logicAssembly = System.Reflection.Assembly.Load("Fonbec.Web.Logic");
TypeAdapterConfig.GlobalSettings.Scan(logicAssembly);

var app = builder.Build();

// Configure the HTTP request pipeline.

await ConfigureMiddleware.ApplyMigrationsAndSeedingAsync(app, applySeeding: false);

await ConfigureMiddleware.SeedRolesAndAdminUserAsync(app);

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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
