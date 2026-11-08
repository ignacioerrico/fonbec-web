using System.Security.Claims;
using Bunit;
using FluentAssertions;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Models.Companies;
using Fonbec.Web.Logic.Models.Companies.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Components.Pages.Companies;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;

namespace Fonbec.Web.Ui.Tests.Components.Companies;

public class CompanyRelationsTests : BunitContext
{
    private readonly ICompanyService _companies = Substitute.For<ICompanyService>();

    public CompanyRelationsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton(_companies);
        var auth = this.AddAuthorization();
        auth.SetAuthorized("Manager");
        auth.SetRoles("Manager");
        auth.SetClaims(new Claim(ClaimTypes.NameIdentifier, "2"));

        _companies.GetCompanyRelationsAsync(1).Returns(new CompanyRelationsViewModel
        {
            CompanyId = 1,
            CompanyName = "Test company",
            Contacts = [new CompanyContactViewModel
            {
                Id = 7,
                FirstName = "Contact",
                Email = "contact@example.test",
                PhoneNumber = "1144445555",
            }],
        });
        _companies.GetSponsorsAvailableToLinkAsync().Returns(new List<SelectableModel<int>>());
        _companies.UpdateCompanyRelationsAsync(Arg.Any<UpdateCompanyRelationsInputModel>())
            .Returns(new UpdateCompanyRelationsResult(true, 1));
        Render<MudPopoverProvider>();
    }

    [Theory]
    [InlineData("Correo electrónico", "invalid-email", "contact@example.test")]
    [InlineData("Teléfono", "abc", "1144445555")]
    [InlineData("Nombre", "", "Contact")]
    [InlineData("Nombre", "   ", "Contact")]
    public void Save_DisablesForInvalidContact_AndEnablesAfterCorrection(
        string label, string invalidValue, string validValue)
    {
        var page = Render<CompanyRelations>(parameters => parameters.Add(p => p.CompanyId, 1));
        SaveDisabled(page).Should().BeFalse();

        Edit(page, label, invalidValue);
        page.WaitForAssertion(() => SaveDisabled(page).Should().BeTrue());

        Edit(page, label, validValue);
        page.WaitForAssertion(() => SaveDisabled(page).Should().BeFalse());
    }

    [Fact]
    public async Task Save_DoesNotPersistInvalidContact_EvenIfHandlerIsInvoked()
    {
        var page = Render<CompanyRelations>(parameters => parameters.Add(p => p.CompanyId, 1));
        Edit(page, "Correo electrónico", "invalid-email");
        var save = page.FindComponents<MudButton>().Single(button => button.Markup.Contains("Guardar"));

        await page.InvokeAsync(() => save.Instance.OnClick.InvokeAsync());

        await _companies.DidNotReceive().UpdateCompanyRelationsAsync(Arg.Any<UpdateCompanyRelationsInputModel>());
    }

    [Fact]
    public async Task Save_AfterCorrection_PersistsTheEditedContact()
    {
        var page = Render<CompanyRelations>(parameters => parameters.Add(p => p.CompanyId, 1));
        Edit(page, "Correo electrónico", "invalid-email");
        Edit(page, "Correo electrónico", "updated@example.test");

        await page.FindAll("button").Single(button => button.TextContent.Trim() == "Guardar")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        await _companies.Received(1).UpdateCompanyRelationsAsync(Arg.Is<UpdateCompanyRelationsInputModel>(
            input => input.CompanyId == 1 && input.UpdatedById == 2
                     && input.Contacts.Count == 1 && input.Contacts[0].Id == 7
                     && input.Contacts[0].Email == "updated@example.test"));
    }

    [Fact]
    public void Save_AddingAnEmptyContactDisablesIt_RemovingThatContactRestoresIt()
    {
        var page = Render<CompanyRelations>(parameters => parameters.Add(p => p.CompanyId, 1));
        page.FindAll("button").Single(button => button.TextContent.Trim() == "Agregar contacto").Click();
        page.WaitForAssertion(() => SaveDisabled(page).Should().BeTrue());

        page.Find("button[aria-label='Quitar a Nuevo contacto']").Click();

        page.WaitForAssertion(() => SaveDisabled(page).Should().BeFalse());
    }

    private static void Edit(IRenderedComponent<CompanyRelations> page, string label, string value) =>
        page.FindComponents<MudTextField<string>>().Single(field => field.Instance.Label == label)
            .Find("input").Input(value);

    private static bool SaveDisabled(IRenderedComponent<CompanyRelations> page) =>
        page.FindAll("button").Single(button => button.TextContent.Trim() == "Guardar").HasAttribute("disabled");
}
