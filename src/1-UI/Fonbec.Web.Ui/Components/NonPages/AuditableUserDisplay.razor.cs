using Fonbec.Web.Logic.Models.Users;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages;

public partial class AuditableUserDisplay
{
    [Parameter, EditorRequired]
    public UsersListViewModel UsersListViewModel { get; set; } = null!;

    [Parameter, EditorRequired]
    public bool IsLastNameFirst { get; set; }

    [Parameter, EditorRequired]
    public int CurrentUserId { get; set; }
}