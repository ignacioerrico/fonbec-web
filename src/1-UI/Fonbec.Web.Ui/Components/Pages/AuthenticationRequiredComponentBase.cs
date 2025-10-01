using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Security.Claims;

namespace Fonbec.Web.Ui.Components.Pages;

public abstract class AuthenticationRequiredComponentBase : ComponentBase
{
    protected bool Loading;

    private protected FonbecClaimModel FonbecClaim { get; private set; } = null!;

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

        var firstName = user.FindFirstValue(FonbecWebUserCustomClaim.FirstName) ?? string.Empty;
        var lastName = user.FindFirstValue(FonbecWebUserCustomClaim.LastName) ?? string.Empty;
        var nickName = user.FindFirstValue(FonbecWebUserCustomClaim.NickName);
        var genderString = user.FindFirstValue(FonbecWebUserCustomClaim.Gender) ?? string.Empty;
        var chapterIdString = user.FindFirstValue(FonbecWebUserCustomClaim.ChapterId);
        var chapterName = user.FindFirstValue(FonbecWebUserCustomClaim.ChapterName);

        var gender = genderString switch
        {
            nameof(Gender.Male) => Gender.Male,
            nameof(Gender.Female) => Gender.Female,
            nameof(Gender.Unknown) or "" => Gender.Unknown,
            _ => throw new ArgumentOutOfRangeException(
                $"Unexpected value '{genderString}' in custom Gender claim where only '{nameof(Gender.Male)}' or '{nameof(Gender.Female)}' are allowed.")
        };

        int? chapterId = int.TryParse(chapterIdString, out var chapterIdParsed)
            ? chapterIdParsed
            : null;

        FonbecClaim = new FonbecClaimModel(
            userId,
            firstName,
            lastName,
            nickName,
            gender,
            chapterId,
            chapterName
        );
    }
}