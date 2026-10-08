namespace Fonbec.Web.Ui.Components.Layout;

/// <summary>
/// Tells the nav badge to reload after a mediador or coordinador marks or undoes a message.
/// </summary>
public sealed class StudentMessageNavCount
{
    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();
}