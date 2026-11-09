using Microsoft.Extensions.Localization;
using MudBlazor;

namespace Fonbec.Web.Ui.Localization;

public sealed class FonbecMudLocalizer(IStringLocalizer<SharedResources> strings) : MudLocalizer
{
    public override LocalizedString this[string key] => strings[key];
}
