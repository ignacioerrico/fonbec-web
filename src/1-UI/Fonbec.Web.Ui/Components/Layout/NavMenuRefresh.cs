namespace Fonbec.Web.Ui.Components.Layout;

/// <summary>
/// Asks the menu to reload its counts after an action that stays on the same page.
/// </summary>
public sealed class NavMenuRefresh
{
    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();
}