using System.Security.Claims;
using Bunit;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Models.Results;
using Fonbec.Web.Logic.Models.Students;
using Fonbec.Web.Logic.Models.Students.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Components.Pages.Students;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;

namespace Fonbec.Web.Ui.Tests.Components.Students;

public class StudentsListTests : BunitContext
{
    [Theory]
    [InlineData("Admin", "Updated notes")]
    [InlineData("Manager", "Updated notes")]
    [InlineData("Admin", "")]
    [InlineData("Manager", "")]
    public async Task Edit_ShowsOnlyEditableFields_AndSavesAdditionalInformation(string role, string notes)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        var auth = this.AddAuthorization();
        auth.SetAuthorized(role);
        auth.SetRoles(role);
        auth.SetPolicies(nameof(StudentCreate));
        auth.SetClaims(new Claim(ClaimTypes.NameIdentifier, "2"));

        var student = new StudentsListViewModel
        {
            StudentId = 7,
            ChapterId = 1,
            StudentFirstName = "Synthetic",
            StudentLastName = "Student",
            StudentEmail = "student@example.test",
            IsStudentActive = true,
            FacilitatorId = 3,
            Notes = "Original notes",
        };
        var students = Substitute.For<IStudentService>();
        students.GetAllStudentsAsync(Arg.Any<int?>()).Returns(new List<StudentsListViewModel> { student });
        students.UpdateStudentAsync(Arg.Any<UpdateStudentInputModel>()).Returns(new CrudResult(1));
        var users = Substitute.For<IUserService>();
        users.GetAllUsersInRoleForSelectionAsync(Arg.Any<string>(), Arg.Any<int?>())
            .Returns(new List<SelectableModel<int>> { new(3, "Synthetic Facilitator") });
        Services.AddSingleton(students);
        Services.AddSingleton(users);
        Render<MudPopoverProvider>();
        var dialogs = Render<MudDialogProvider>();
        var page = Render<StudentsList>();

        page.WaitForElement("tbody tr td:last-child button").Click();
        dialogs.WaitForElement(".mud-dialog");
        Assert.DoesNotContain(dialogs.FindComponents<MudTextField<string>>(), field => field.Instance.Label == "Notas");
        dialogs.FindComponents<MudTextField<string>>()
            .Single(field => field.Instance.Label == "Información adicional").Find("input").Input(notes);
        await dialogs.FindAll("button").Single(button => button.TextContent.Trim() == "Save")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        await students.Received(1).UpdateStudentAsync(Arg.Is<UpdateStudentInputModel>(
            input => input.StudentId == 7 && input.UpdatedById == 2 && input.StudentNotes == notes));
    }
}
