using Fonbec.Web.DataAccess.Entities.Enums;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class GenderSelector
{
    [Parameter]
    public Gender SelectedGender { get; set; }

    [Parameter]
    public EventCallback<Gender> SelectedGenderChanged { get; set; }

    private async Task OnSelectedValueChanged(Gender selectedGender)
    {
        await SelectedGenderChanged.InvokeAsync(selectedGender);
    }

    protected override async Task OnParametersSetAsync()
    {
        switch (SelectedGender)
        {
            case Gender.Unknown:
                // If no gender is selected at invocation time, set it to a default (either male or female).
                SelectedGender = Gender.Male;
                await SelectedGenderChanged.InvokeAsync(SelectedGender);
                break;
            case Gender.Male:
                SelectedGender = Gender.Male;
                break;
            case Gender.Female:
                SelectedGender = Gender.Female;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        await base.OnParametersSetAsync();
    }
}