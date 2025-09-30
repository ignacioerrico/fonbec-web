namespace Fonbec.Web.Ui.Models.User;

internal class UserPermissionsBindModel
{
    public bool IsChecked { get; set; }

    public string Page { get; init; } = null!;

    public string Description { get; init; } = null!;
}