using FluentAssertions;
using Fonbec.Web.Logic.Models.Facilitators;

namespace Fonbec.Web.Logic.Tests.Models.Facilitators;

public class StudentsDashboardViewModelTests
{
    [Fact]
    public void CurrentPlanLabel_Is_Null_When_No_Plan()
    {
        var viewModel = new StudentsDashboardViewModel();

        viewModel.CurrentPlanLabel.Should().BeNull();
    }

    [Fact]
    public void CurrentPlanLabel_Formats_Month_And_Year_In_Spanish()
    {
        var viewModel = new StudentsDashboardViewModel
        {
            CurrentPlanStartsOn = new DateTime(2026, 6, 1),
        };

        viewModel.CurrentPlanLabel.Should().Be("Jun 2026");
    }
}
