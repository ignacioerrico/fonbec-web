using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;
using Fonbec.Web.Logic.Models.PlannedDeliveries.Input;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.PlannedDeliveries.Input;

public class CreatePlannedDeliveryInputModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_All_Fields_Correctly_When_All_Values_Present()
    {
        var input = new CreatePlannedDeliveryInputModel(
            ChapterId: 314,
            PlanStartsOn: new DateTime(1996, 6, 1),
            PlanNotes: "Some important notes",
            CreatedById: 512
        );

        var result = input.Adapt<CreatePlannedDeliveryInputDataModel>(Config);

        result.ChapterId.Should().Be(314);
        result.PlanStartsOn.Should().Be(new DateTime(1996, 6, 1));
        result.PlanNotes.Should().Be("Some important notes");
        result.CreatedById.Should().Be(512);
    }

    [Fact]
    public void InputModel_Notes_AreTrimmed_ButNotNormalized()
    {
        var input = new CreatePlannedDeliveryInputModel(
            ChapterId: 314,
            PlanStartsOn: new DateTime(1996, 6, 1),
            PlanNotes: "  some impoRTAnt notes   ",
            CreatedById: 512
        );

        var result = input.Adapt<CreatePlannedDeliveryInputDataModel>(Config);

        result.PlanNotes.Should().Be("some impoRTAnt notes");
    }

    [Fact]
    public void InputModel_Notes_AreNull_WhenEmpty()
    {
        var input = new CreatePlannedDeliveryInputModel(
            ChapterId: 314,
            PlanStartsOn: new DateTime(1996, 6, 1),
            PlanNotes: "   ",
            CreatedById: 512
        );

        var result = input.Adapt<CreatePlannedDeliveryInputDataModel>(Config);

        result.PlanNotes.Should().BeNull();
    }
}