using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.PlannedDelivery;
using Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.PlannedDeliveries.Input;
using Fonbec.Web.Logic.Services;
using Mapster;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class PlannedDeliveryServiceTests
{
    private readonly IPlannedDeliveryRepository _repository = Substitute.For<IPlannedDeliveryRepository>();
    private readonly ILetterPlanProgressRepository _progressRepository = Substitute.For<ILetterPlanProgressRepository>();
    private readonly PlannedDeliveryService _service;

    public PlannedDeliveryServiceTests()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(PlannedDeliveryService).Assembly);
        _service = new PlannedDeliveryService(_repository, _progressRepository);
    }

    [Fact]
    public async Task CreatePlannedDeliveryAsync_Rejects_When_Incomplete_Plan_Exists()
    {
        _repository.HasIncompletePlanAsync(5).Returns(true);

        var result = await _service.CreatePlannedDeliveryAsync(new CreatePlannedDeliveryInputModel(
            5, new DateTime(2026, 8, 1), "Notas", 1));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(PlannedDeliveryService.IncompletePlanAlreadyExists);
        await _repository.DidNotReceive().CreatePlannedDeliveryAsync(Arg.Any<CreatePlannedDeliveryInputDataModel>());
    }

    [Fact]
    public async Task CreatePlannedDeliveryAsync_Rejects_When_No_Slots()
    {
        _repository.HasIncompletePlanAsync(5).Returns(false);
        _progressRepository.CountRequiredSlotsAsync(5, Arg.Any<DateTime>()).Returns(0);

        var result = await _service.CreatePlannedDeliveryAsync(new CreatePlannedDeliveryInputModel(
            5, new DateTime(2026, 8, 1), "Notas", 1));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(PlannedDeliveryService.NoSlotsForMonth);
        await _repository.DidNotReceive().CreatePlannedDeliveryAsync(Arg.Any<CreatePlannedDeliveryInputDataModel>());
    }

    [Fact]
    public async Task CreatePlannedDeliveryAsync_Creates_When_No_Incomplete_Plan()
    {
        _repository.HasIncompletePlanAsync(5).Returns(false);
        _progressRepository.CountRequiredSlotsAsync(5, Arg.Any<DateTime>()).Returns(2);
        _repository.CreatePlannedDeliveryAsync(Arg.Any<CreatePlannedDeliveryInputDataModel>()).Returns(1);

        var result = await _service.CreatePlannedDeliveryAsync(new CreatePlannedDeliveryInputModel(
            5, new DateTime(2026, 8, 1), "Notas", 1));

        result.IsSuccess.Should().BeTrue();
        result.AnyAffectedRows.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentPlanAsync_Maps_Repository_Result()
    {
        _repository.GetCurrentPlanAsync(5).Returns(new CurrentPlannedDeliveryDataModel
        {
            PlannedDeliveryId = 10,
            PlannedDeliveryStartsOn = new DateTime(2026, 3, 1),
            IsPlannedDeliveryCompleted = false,
        });

        var result = await _service.GetCurrentPlanAsync(5);

        result.Should().NotBeNull();
        result!.PlannedDeliveryId.Should().Be(10);
        result.PlannedDeliveryStartsOnText.Should().Contain("2026");
    }

    [Fact]
    public async Task UpdatePlannedDeliveryAsync_Rejects_Date_Change_When_Completed()
    {
        _repository.GetPlanMetadataAsync(10).Returns(new PlannedDeliveryMetadataDataModel
        {
            PlannedDeliveryId = 10,
            ChapterId = 5,
            StartsOn = new DateTime(2026, 3, 1),
            Completed = true,
        });

        var result = await _service.UpdatePlannedDeliveryAsync(new UpdatePlannedDeliveryInputModel(
            10, new DateTime(2026, 4, 1), "Notas", 1));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(PlannedDeliveryService.CannotChangeCompletedPlanDate);
        await _repository.DidNotReceive().UpdatePlannedDeliveryAsync(Arg.Any<UpdatePlannedDeliveryInputDataModel>());
    }
}