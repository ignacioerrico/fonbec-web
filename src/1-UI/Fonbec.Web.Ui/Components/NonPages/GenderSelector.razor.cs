using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages;

public partial class GenderSelector
{
    private readonly SelectableModel<Gender> _male = new(Gender.Male, "Masculino");
    private readonly SelectableModel<Gender> _female = new(Gender.Female, "Femenino");

    private SelectableModel<Gender> _selectedGender = null!;

    [Parameter]
    public Gender SelectedGender { get; set; }

    [Parameter]
    public EventCallback<Gender> SelectedGenderChanged { get; set; }

    private async Task OnSelectedValuesChanged(IEnumerable<SelectableModel<Gender>?>? selectedGenders)
    {
        await SelectedGenderChanged.InvokeAsync(selectedGenders!.Single()!.Key);
    }

    protected override async Task OnParametersSetAsync()
    {
        switch (SelectedGender)
        {
            case Gender.Unknown:
                // If no gender is selected at invocation time, set it to a default (either male or female).
                _selectedGender = _male;
                await SelectedGenderChanged.InvokeAsync(_selectedGender.Key);
                break;
            case Gender.Male:
                _selectedGender = _male;
                break;
            case Gender.Female:
                _selectedGender = _female;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        await base.OnParametersSetAsync();
    }
}