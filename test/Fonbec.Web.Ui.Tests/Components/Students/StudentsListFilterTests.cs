using System.Security.Claims;
using Bunit;
using Fonbec.Web.Logic.Models.Students;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Components.Pages.Students;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;

namespace Fonbec.Web.Ui.Tests.Components.Students;

public class StudentsListFilterTests : BunitContext
{
    private const string StorageKey = "students-filters:v1:7:";

    public StudentsListFilterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();
        Services.AddScoped<ProtectedSessionStorage>();

        var auth = this.AddAuthorization();
        auth.SetAuthorized("Admin");
        auth.SetRoles("Admin");
        auth.SetClaims(new Claim(ClaimTypes.NameIdentifier, "7"));

        var students = Substitute.For<IStudentService>();
        students.GetAllStudentsAsync(null).Returns(Enumerable.Range(1, 12)
            .Select(id => Student(id, "Ana", "Mediadora Uno"))
            .Append(Student(13, "Bruno", "Mediador Dos"))
            .ToList());
        Services.AddSingleton(students);
        Render<MudPopoverProvider>();
    }

    [Fact]
    public async Task AppliedFiltersSurvivePaginationAndRemountUntilCleared()
    {
        var getStorage = JSInterop.Setup<string?>("sessionStorage.getItem", StorageKey);
        getStorage.SetResult(null);
        var page = Render<StudentsList>();
        var search = page.WaitForElement("input[placeholder='Buscar']");
        search.Input("Ana");

        var grid = page.FindComponent<StudentFilterDataGrid<StudentsListViewModel>>();
        NameFilter(grid).Input("Ana");

        page.WaitForAssertion(() =>
        {
            Assert.Equal(12, grid.Instance.FilteredItems.Count());
            Assert.NotEmpty(grid.Instance.FilterDefinitions);
            Assert.True(JSInterop.Invocations["sessionStorage.setItem"].Count >= 2);
        });
        await page.InvokeAsync(() => grid.Instance.NavigateTo(Page.Next));
        Assert.Equal(1, grid.Instance.CurrentPage);
        Assert.Equal(12, grid.Instance.FilteredItems.Count());

        var saved = (string)JSInterop.Invocations["sessionStorage.setItem"].Last().Arguments[1]!;
        Assert.DoesNotContain("Ana", saved);
        page.Dispose();
        getStorage.SetResult(saved);
        page = Render<StudentsList>();
        grid = page.FindComponent<StudentFilterDataGrid<StudentsListViewModel>>();
        page.WaitForAssertion(() =>
        {
            Assert.Equal("Ana", page.Find("input[placeholder='Buscar']").GetAttribute("value"));
            Assert.Equal("Ana", Assert.Single(grid.Instance.FilterDefinitions).Value);
            Assert.Equal(12, grid.Instance.FilteredItems.Count());
        });

        page.Find("input[placeholder='Buscar']").Input(string.Empty);
        NameFilter(grid).Input(string.Empty);
        page.WaitForAssertion(() =>
        {
            Assert.Equal(13, grid.Instance.FilteredItems.Count());
            Assert.Equal(StorageKey, Assert.Single(JSInterop.Invocations["sessionStorage.removeItem"]).Arguments[0]);
        });
        page.Dispose();
        getStorage.SetResult(null);

        var clearedPage = Render<StudentsList>();
        Assert.Equal(13, clearedPage.FindComponent<StudentFilterDataGrid<StudentsListViewModel>>().Instance.FilteredItems.Count());
    }

    private static AngleSharp.Dom.IElement NameFilter(IRenderedComponent<StudentFilterDataGrid<StudentsListViewModel>> grid) =>
        grid.FindComponents<FilterHeaderCell<StudentsListViewModel>>()
            .Single(cell => Equals(cell.Instance.Column.Tag, "name")).Find("input");

    private static StudentsListViewModel Student(int id, string name, string facilitator) => new()
    {
        StudentId = id,
        StudentFirstName = name,
        StudentLastName = $"Test {id}",
        StudentChapterName = "Filial de prueba",
        FacilitatorFullName = facilitator,
        IsStudentActive = true,
    };
}
