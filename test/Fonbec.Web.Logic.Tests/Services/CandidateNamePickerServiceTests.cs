using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Review;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Review;
using Fonbec.Web.Logic.Services;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class CandidateNamePickerServiceTests
{
    private const long DocumentId = 42;

    private readonly IStudentRepository _studentRepository = Substitute.For<IStudentRepository>();
    private readonly ISponsorRepository _sponsorRepository = Substitute.For<ISponsorRepository>();
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();

    private CandidateNamePickerService CreateService() =>
        new(_studentRepository, _sponsorRepository, _companyRepository);

    private static CandidateNameDataModel Person(int id) =>
        new() { Id = id, FirstName = $"First{id}", LastName = $"Last{id}", IsCompany = false };

    private static CandidateNameDataModel Company(int id, string name) =>
        new() { Id = id, FirstName = name, LastName = string.Empty, IsCompany = true };

    private static CandidateNameKey PersonKey(int id) => new(IsCompany: false, id);

    private static CandidateNameKey CompanyKey(int id) => new(IsCompany: true, id);

    [Fact]
    public async Task AddresseeChoices_PersonLetter_MixesPersonAndCompanyDistractors()
    {
        const int correctId = 100;
        _sponsorRepository.GetSponsorNameAsync(correctId).Returns(Person(correctId));
        _sponsorRepository.GetSponsorCandidateNamesAsync(correctId)
            .Returns([Person(1), Person(2), Person(3), Person(4)]);
        _companyRepository.GetCompanyCandidateNamesAsync(null)
            .Returns([Company(10, "Acme"), Company(11, "Beta"), Company(12, "Gamma"), Company(13, "Delta")]);

        var service = CreateService();

        var result = await service.GetAddresseeNameChoicesAsync(DocumentId, correctId, companyId: null, count: 5);

        result.CorrectIsCompany.Should().BeFalse();
        result.CorrectId.Should().Be(correctId);
        result.CorrectKey.Should().Be(PersonKey(correctId));
        result.Names.Should().HaveCount(5);
        result.Names.Select(n => n.Key).Should().OnlyHaveUniqueItems();
        result.Names.Should().Contain(n => n.Key == PersonKey(correctId));
        result.Names.Should().Contain(n => n.IsCompany);
        result.Names.Should().Contain(n => !n.IsCompany && n.Id != correctId);

        await _sponsorRepository.Received(1).GetSponsorCandidateNamesAsync(correctId);
        await _companyRepository.Received(1).GetCompanyCandidateNamesAsync(null);
    }

    [Fact]
    public async Task AddresseeChoices_CompanyLetter_MixesPersonAndCompanyDistractors()
    {
        const int correctId = 50;
        _companyRepository.GetCompanyNameAsync(correctId).Returns(Company(correctId, "Acme SA"));
        _companyRepository.GetCompanyCandidateNamesAsync(correctId)
            .Returns([Company(1, "Beta"), Company(2, "Gamma")]);
        _sponsorRepository.GetSponsorCandidateNamesAsync(null)
            .Returns([Person(1), Person(2), Person(3), Person(4)]);

        var service = CreateService();

        var result = await service.GetAddresseeNameChoicesAsync(DocumentId, sponsorId: null, correctId, count: 5);

        result.CorrectIsCompany.Should().BeTrue();
        result.CorrectKey.Should().Be(CompanyKey(correctId));
        result.Names.Should().HaveCount(5);
        result.Names.Single(n => n.Key == CompanyKey(correctId)).DisplayName.Should().Be("Acme SA");
        result.Names.Should().Contain(n => !n.IsCompany);
        await _companyRepository.Received(1).GetCompanyCandidateNamesAsync(correctId);
        await _sponsorRepository.Received(1).GetSponsorCandidateNamesAsync(null);
    }

    [Fact]
    public async Task AddresseeChoices_CollidingNumericIds_TreatsOnlyMatchingKindAsCorrect()
    {
        const int collidingId = 5;
        _sponsorRepository.GetSponsorNameAsync(collidingId).Returns(Person(collidingId));
        _sponsorRepository.GetSponsorCandidateNamesAsync(collidingId).Returns([Person(6)]);
        _companyRepository.GetCompanyCandidateNamesAsync(null).Returns([Company(collidingId, "Acme")]);

        var service = CreateService();

        var result = await service.GetAddresseeNameChoicesAsync(DocumentId, collidingId, companyId: null, count: 3);

        result.CorrectKey.Should().Be(PersonKey(collidingId));
        result.Names.Should().Contain(n => n.Key == PersonKey(collidingId));
        result.Names.Should().Contain(n => n.Key == CompanyKey(collidingId));
        result.Names.Select(n => n.Key).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task AddresseeChoices_SparseCompanies_StillFillsFromPersonSponsors()
    {
        const int correctId = 1;
        _companyRepository.GetCompanyNameAsync(correctId).Returns(Company(correctId, "Only Co"));
        _companyRepository.GetCompanyCandidateNamesAsync(correctId).Returns([Company(2, "Other Co")]);
        _sponsorRepository.GetSponsorCandidateNamesAsync(null)
            .Returns([Person(10), Person(11), Person(12), Person(13)]);

        var service = CreateService();

        var result = await service.GetAddresseeNameChoicesAsync(DocumentId, sponsorId: null, correctId, count: 5);

        result.Names.Should().HaveCount(5);
        result.Names.Count(n => n.IsCompany).Should().BeGreaterThanOrEqualTo(1);
        result.Names.Count(n => !n.IsCompany).Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task AddresseeChoices_WithCount1_ReturnsOnlyCorrectAndRequestsNoPools()
    {
        const int correctId = 42;
        _sponsorRepository.GetSponsorNameAsync(correctId).Returns(Person(correctId));

        var service = CreateService();

        var result = await service.GetAddresseeNameChoicesAsync(DocumentId, correctId, companyId: null, count: 1);

        result.Names.Should().ContainSingle().Which.Key.Should().Be(PersonKey(correctId));
        await _sponsorRepository.DidNotReceive().GetSponsorCandidateNamesAsync(Arg.Any<int?>());
        await _companyRepository.DidNotReceive().GetCompanyCandidateNamesAsync(Arg.Any<int?>());
    }

    [Fact]
    public async Task StudentChoices_WithCount5_ReturnsCorrectPlusFourDistinctDistractors()
    {
        const int correctId = 7;
        _studentRepository.GetStudentNameAsync(correctId).Returns(Person(correctId));
        _studentRepository.GetStudentCandidateNamesAsync(correctId)
            .Returns([Person(11), Person(12), Person(13), Person(14)]);

        var service = CreateService();

        var result = await service.GetStudentNameChoicesAsync(DocumentId, correctId, 5);

        result.CorrectId.Should().Be(correctId);
        result.CorrectIsCompany.Should().BeFalse();
        result.Names.Should().HaveCount(5);
        result.Names.Select(n => n.Id).Should().Contain(correctId);
        await _studentRepository.Received(1).GetStudentCandidateNamesAsync(correctId);
    }

    [Fact]
    public async Task AddresseeChoices_NeitherOrBothRecipients_Throws()
    {
        var service = CreateService();

        var neither = async () => await service.GetAddresseeNameChoicesAsync(DocumentId, null, null, 5);
        var both = async () => await service.GetAddresseeNameChoicesAsync(DocumentId, 1, 2, 5);

        await neither.Should().ThrowAsync<ArgumentException>();
        await both.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddresseeChoices_SameDocument_ReturnsIdenticalNamesAndOrder()
    {
        const int correctId = 100;
        _sponsorRepository.GetSponsorNameAsync(correctId).Returns(Person(correctId));
        _sponsorRepository.GetSponsorCandidateNamesAsync(correctId)
            .Returns([Person(1), Person(2), Person(3), Person(4)]);
        _companyRepository.GetCompanyCandidateNamesAsync(null)
            .Returns([Company(10, "Acme"), Company(11, "Beta"), Company(12, "Gamma"), Company(13, "Delta")]);

        var service = CreateService();

        var first = await service.GetAddresseeNameChoicesAsync(DocumentId, correctId, companyId: null, count: 5);
        var second = await service.GetAddresseeNameChoicesAsync(DocumentId, correctId, companyId: null, count: 5);

        first.Names.Select(n => n.Key).Should().Equal(second.Names.Select(n => n.Key));
    }

    [Fact]
    public async Task StudentChoices_SameDocument_ReturnsIdenticalNamesAndOrder()
    {
        const int correctId = 7;
        _studentRepository.GetStudentNameAsync(correctId).Returns(Person(correctId));
        _studentRepository.GetStudentCandidateNamesAsync(correctId)
            .Returns([Person(11), Person(12), Person(13), Person(14)]);

        var service = CreateService();

        var first = await service.GetStudentNameChoicesAsync(DocumentId, correctId, 5);
        var second = await service.GetStudentNameChoicesAsync(DocumentId, correctId, 5);

        first.Names.Select(n => n.Key).Should().Equal(second.Names.Select(n => n.Key));
    }

    [Fact]
    public async Task AddresseeChoices_DifferentDocuments_ProduceDifferentLists()
    {
        const int correctId = 100;
        _sponsorRepository.GetSponsorNameAsync(correctId).Returns(Person(correctId));
        _sponsorRepository.GetSponsorCandidateNamesAsync(correctId)
            .Returns([Person(1), Person(2), Person(3), Person(4), Person(5), Person(6)]);
        _companyRepository.GetCompanyCandidateNamesAsync(null)
            .Returns([Company(10, "Acme"), Company(11, "Beta"), Company(12, "Gamma"), Company(13, "Delta")]);

        var service = CreateService();

        var first = await service.GetAddresseeNameChoicesAsync(1, correctId, companyId: null, count: 5);
        var second = await service.GetAddresseeNameChoicesAsync(99999, correctId, companyId: null, count: 5);

        first.Names.Select(n => n.Key).Should().NotEqual(second.Names.Select(n => n.Key));
        first.Names.Should().Contain(n => n.Key == PersonKey(correctId));
        second.Names.Should().Contain(n => n.Key == PersonKey(correctId));
    }
}
