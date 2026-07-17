using FluentAssertions;
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
    public async Task GetExemptStudentIdsForPlanAsync_ReturnsSetFromRepository()
    {
        _repository.GetActiveExemptStudentIdsForPlanAsync(7).Returns([10, 11, 11]);

        var result = await _service.GetExemptStudentIdsForPlanAsync(7);

        result.Should().BeEquivalentTo(new HashSet<int> { 10, 11 });
    }
}