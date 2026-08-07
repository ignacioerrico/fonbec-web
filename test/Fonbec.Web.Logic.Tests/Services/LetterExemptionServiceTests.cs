using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.LetterExemptions;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Services;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class LetterExemptionServiceTests
{
    private readonly ILetterExemptionRepository _repository = Substitute.For<ILetterExemptionRepository>();
    private readonly LetterExemptionService _service;

    public LetterExemptionServiceTests()
    {
        _service = new LetterExemptionService(_repository);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsExemptAsync_ReflectsRepository(bool exempt)
    {
        _repository.IsActiveExemptionAsync(5, 3).Returns(exempt);

        var result = await _service.IsExemptAsync(5, 3);

        result.Should().Be(exempt);
    }

    [Fact]
    public async Task GetActiveExemptionReasonsForPlanAsync_ReturnsReasonsByStudent_KeepingFirstForDuplicates()
    {
        _repository.GetActiveExemptionsForPlanAsync(7).Returns([
            new LetterExemptionReasonDataModel { StudentId = 10, Reason = "Viaje" },
            new LetterExemptionReasonDataModel { StudentId = 11, Reason = "Enfermedad" },
            new LetterExemptionReasonDataModel { StudentId = 11, Reason = "Duplicado" },
        ]);

        var result = await _service.GetActiveExemptionReasonsForPlanAsync(7);

        result.Should().BeEquivalentTo(new Dictionary<int, string>
        {
            [10] = "Viaje",
            [11] = "Enfermedad",
        });
    }
}