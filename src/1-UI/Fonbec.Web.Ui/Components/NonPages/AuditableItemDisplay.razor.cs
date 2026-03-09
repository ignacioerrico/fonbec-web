using Fonbec.Web.Logic.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages;

public partial class AuditableItemDisplay
{
    [Parameter, EditorRequired]
    public int Id { get; set; }

    [Parameter, EditorRequired]
    public string ItemName { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string? Email { get; set; }

    [Parameter, EditorRequired]
    public bool IsActive { get; set; }

    [Parameter]
    public Color Color { get; set; } = Color.Primary;

    [Parameter, EditorRequired]
    public AuditableViewModel AuditableViewModel { get; set; } = null!;
}