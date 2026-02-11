using Fonbec.Web.Logic.Models.Users;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages;


public partial class EditPersonDialog<T>
{
    [Parameter]
    public DataGridEditFormContext<T> Context { get; set; } = default!;

    private UsersListViewModel _original = new();
    protected UsersListViewModel _user = new();

    protected override void OnInitialized()
    {
        _user = Context.Item;
        _original = new UsersListViewModel
        {
            UserFirstName = _user.UserFirstName,
            UserLastName = _user.UserLastName,
            UserNickName = _user.UserNickName,
            UserGender = _user.UserGender,
            UserEmail = _user.UserEmail,
            UserPhoneNumber = _user.UserPhoneNumber,
            UserNotes = _user.UserNotes,

        };
    }

    protected bool IsSaveDisabled =>
        _user.UserFirstName == _original.UserFirstName
        && _user.UserLastName == _original.UserLastName
        && _user.UserNickName == _original.UserNickName
        && _user.UserGender == _original.UserGender
        && _user.UserEmail == _original.UserEmail
        && _user.UserPhoneNumber == _original.UserPhoneNumber
        && _user.UserNotes == _original.UserNotes;
    protected void OnFieldChanged()
    {
        if(IsSaveDisabled)
        {
            Context.CancelEdit();
        }
    }
}
