using System.Security.Claims;
using Bunit;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Models.Results;
using Fonbec.Web.Logic.Models.Sponsors;
using Fonbec.Web.Logic.Models.Sponsors.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Components.Pages.Sponsors;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;

namespace Fonbec.Web.Ui.Tests.Components.Sponsors;

public class SponsorsListTests : BunitContext
{
    [Theory]
    [InlineData("Admin", false, "Updated notes")]
    [InlineData("Admin", true, "Updated notes")]
    [InlineData("Manager", false, "Updated notes")]
    [InlineData("Manager", true, "Updated notes")]
    [InlineData("Admin", false, "")]
    [InlineData("Manager", false, "")]
    public async Task Save_SubmitsNotesOnlyAndCombinedEdits_IncludingClearing(
        string role, bool changeLastName, string notes)
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        var auth = this.AddAuthorization();
        auth.SetAuthorized(role);
        auth.SetRoles(role);
        auth.SetPolicies(nameof(SponsorCreate));
        auth.SetClaims(new Claim(ClaimTypes.NameIdentifier, "2"));

        var sponsor = new SponsorsListViewModel
        {
            SponsorId = 7,
            SponsorFirstName = "First",
            SponsorLastName = "Last",
            SponsorNickName = "",
            SponsorPhoneNumber = "",
            SponsorEmail = "sponsor@example.test",
            IsSponsorActive = true,
            Notes = "Original notes",
        };
        var sponsors = Substitute.For<ISponsorService>();
        sponsors.GetAllSponsorsAsync(Arg.Any<int?>()).Returns(new List<SponsorsListViewModel> { sponsor });
        sponsors.UpdateSponsorAsync(Arg.Any<UpdateSponsorInputModel>()).Returns(new CrudResult(1));
        var companies = Substitute.For<ICompanyService>();
        companies.GetAllCompaniesForSelectionAsync().Returns(new List<SelectableModel<int>>());
        Services.AddSingleton(sponsors);
        Services.AddSingleton(companies);
        Render<MudPopoverProvider>();
        var dialogs = Render<MudDialogProvider>();
        var page = Render<SponsorsList>();

        page.WaitForElement("tbody tr td:last-child button").Click();
        dialogs.WaitForElement(".mud-dialog");
        dialogs.FindComponents<MudTextField<string>>()
            .Single(field => field.Instance.Label == "Información adicional").Find("input").Input(notes);
        if (changeLastName)
        {
            dialogs.FindComponents<MudTextField<string>>()
                .Single(field => field.Instance.Label == "Apellido").Find("input").Input("Changed");
        }
        await dialogs.FindAll("button").Single(button => button.TextContent.Trim() == "Save")
            .ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        await sponsors.Received(1).UpdateSponsorAsync(Arg.Is<UpdateSponsorInputModel>(
            input => input.SponsorId == 7 && input.UpdatedById == 2 && input.SponsorNotes == notes
                     && input.SponsorLastName == (changeLastName ? "Changed" : "Last")));
    }
}
