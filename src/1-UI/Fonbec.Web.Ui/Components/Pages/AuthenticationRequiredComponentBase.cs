using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Security.Claims;

namespace Fonbec.Web.Ui.Components.Pages;

public abstract class AuthenticationRequiredComponentBase : ComponentBase
{
    protected bool Loading;

    protected int CurrentUserId { get; private set; }

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    [Inject]
    protected ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    protected NavigationManager NavigationManager { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        await GetAuthenticatedUserIdAsync();
    }

    protected async Task GetAuthenticatedUserIdAsync()
    {
        if (AuthenticationState is null)
        {
            Loading = false;
            Snackbar.Add("AuthenticationState is null.", Severity.Error);
            NavigationManager.NavigateTo(NavRoutes.Home);
            return;
        }

        var user = (await AuthenticationState).User;

        if (user.Identity is not { IsAuthenticated: true })
        {
            Loading = false;
            Snackbar.Add("El usuario no está autenticado.", Severity.Error);
            NavigationManager.NavigateTo(NavRoutes.Home);
            return;
        }

        var userIdString = user.FindFirstValue(ClaimTypes.NameIdentifier)
                           ?? throw new NullReferenceException("UserId not found among user claims.");

        if (!int.TryParse(userIdString, out var userId))
        {
            throw new ArgumentException($"UserId value '{userIdString}' is not an integer.");
        }

        CurrentUserId = userId;
    }
}