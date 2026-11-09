using System.Security.Claims;
using Bunit;
using Fonbec.Web.Logic.Models.Facilitators;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Components.Pages.Facilitators;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using NSubstitute;

namespace Fonbec.Web.Ui.Tests.Components.Facilitators;

public class HistoryParameterReproductionTests : BunitContext
{
    private readonly IFacilitatorService _service = Substitute.For<IFacilitatorService>();

    public HistoryParameterReproductionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton(_service);
        var auth = this.AddAuthorization();
        auth.SetAuthorized("Uploader");
        auth.SetRoles("Uploader");
        auth.SetClaims(new Claim(ClaimTypes.NameIdentifier, "2"));
    }

    [Fact]
    public void Letters_ChangingStudentParameter_LoadsNewStudent()
    {
        _service.GetLettersHistoryAsync(2, 101).Returns(new LettersHistoryViewModel { StudentId = 101, StudentName = "Synthetic Alpha" });
        _service.GetLettersHistoryAsync(2, 202).Returns(new LettersHistoryViewModel { StudentId = 202, StudentName = "Synthetic Beta" });
        var page = Render<FacilitatorLetters>(p => p.Add(x => x.StudentId, 101));
        Assert.Contains("Synthetic Alpha", page.Markup);
        page.Render(p => p.Add(x => x.StudentId, 202));
        Assert.Contains("Synthetic Beta", page.Markup);
        Assert.DoesNotContain("Synthetic Alpha", page.Markup);
    }

    [Fact]
    public void ReportCards_ChangingStudentParameter_LoadsNewStudent()
    {
        _service.GetReportCardsHistoryAsync(2, 101).Returns(new ReportCardsHistoryViewModel { StudentId = 101, StudentName = "Synthetic Alpha" });
        _service.GetReportCardsHistoryAsync(2, 202).Returns(new ReportCardsHistoryViewModel { StudentId = 202, StudentName = "Synthetic Beta" });
        var page = Render<FacilitatorReportCards>(p => p.Add(x => x.StudentId, 101));
        Assert.Contains("Synthetic Alpha", page.Markup);
        page.Render(p => p.Add(x => x.StudentId, 202));
        Assert.Contains("Synthetic Beta", page.Markup);
        Assert.DoesNotContain("Synthetic Alpha", page.Markup);
    }

    [Fact]
    public void Letters_InaccessibleStudent_ClearsPreviousHistory()
    {
        _service.GetLettersHistoryAsync(2, 101).Returns(new LettersHistoryViewModel { StudentName = "Synthetic Alpha" });
        _service.GetLettersHistoryAsync(2, 202).Returns((LettersHistoryViewModel?)null);
        var page = Render<FacilitatorLetters>(p => p.Add(x => x.StudentId, 101));
        page.Render(p => p.Add(x => x.StudentId, 202));
        Assert.DoesNotContain("Synthetic Alpha", page.Markup);
        Assert.EndsWith("/mis-becarios", Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().Uri);
    }

    [Fact]
    public void ReportCards_InaccessibleStudent_ClearsPreviousHistory()
    {
        _service.GetReportCardsHistoryAsync(2, 101).Returns(new ReportCardsHistoryViewModel { StudentName = "Synthetic Alpha" });
        _service.GetReportCardsHistoryAsync(2, 202).Returns((ReportCardsHistoryViewModel?)null);
        var page = Render<FacilitatorReportCards>(p => p.Add(x => x.StudentId, 101));
        page.Render(p => p.Add(x => x.StudentId, 202));
        Assert.DoesNotContain("Synthetic Alpha", page.Markup);
        Assert.EndsWith("/mis-becarios", Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().Uri);
    }

    [Fact]
    public async Task Letters_SlowPreviousRequest_DoesNotReplaceCurrentStudent()
    {
        var pending = new TaskCompletionSource<LettersHistoryViewModel?>();
        _service.GetLettersHistoryAsync(2, 101).Returns(new LettersHistoryViewModel { StudentName = "Synthetic Alpha" });
        _service.GetLettersHistoryAsync(2, 202).Returns(pending.Task);
        _service.GetLettersHistoryAsync(2, 303).Returns(new LettersHistoryViewModel { StudentName = "Synthetic Gamma" });
        var page = Render<FacilitatorLetters>(p => p.Add(x => x.StudentId, 101));
        page.Render(p => p.Add(x => x.StudentId, 202));
        Assert.DoesNotContain("Synthetic Alpha", page.Markup);
        page.Render(p => p.Add(x => x.StudentId, 303));
        await page.InvokeAsync(() => pending.SetResult(new LettersHistoryViewModel { StudentName = "Synthetic Beta" }));
        page.WaitForAssertion(() => Assert.Contains("Synthetic Gamma", page.Markup));
        Assert.DoesNotContain("Synthetic Beta", page.Markup);
    }

    [Fact]
    public async Task ReportCards_SlowPreviousRequest_DoesNotRedirectCurrentStudent()
    {
        var pending = new TaskCompletionSource<ReportCardsHistoryViewModel?>();
        _service.GetReportCardsHistoryAsync(2, 101).Returns(new ReportCardsHistoryViewModel { StudentName = "Synthetic Alpha" });
        _service.GetReportCardsHistoryAsync(2, 202).Returns(pending.Task);
        _service.GetReportCardsHistoryAsync(2, 303).Returns(new ReportCardsHistoryViewModel { StudentName = "Synthetic Gamma" });
        var page = Render<FacilitatorReportCards>(p => p.Add(x => x.StudentId, 101));
        page.Render(p => p.Add(x => x.StudentId, 202));
        Assert.DoesNotContain("Synthetic Alpha", page.Markup);
        page.Render(p => p.Add(x => x.StudentId, 303));
        var navigation = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        var uri = navigation.Uri;
        await page.InvokeAsync(() => pending.SetResult(null));
        page.WaitForAssertion(() => Assert.Contains("Synthetic Gamma", page.Markup));
        Assert.Equal(uri, navigation.Uri);
    }
}

