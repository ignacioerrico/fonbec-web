using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Sponsors;
using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Sponsors;
using Fonbec.Web.Logic.Models.Sponsors.Input;
using Fonbec.Web.Logic.Services;
using Mapster;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class SponsorServiceSendAlsoToTests
{
    private readonly ISponsorRepository _sponsorRepository;
    private readonly SponsorService _sponsorService;

    public SponsorServiceSendAlsoToTests()
    {
        _sponsorRepository = Substitute.For<ISponsorRepository>();
        _sponsorService = new SponsorService(_sponsorRepository);
        TypeAdapterConfig.GlobalSettings.Scan(typeof(CreateSponsorInputModel).Assembly);
    }

    [Fact]
    public async Task CreateSponsorAsync_Allows_Empty_Recipient_List()
    {
        _sponsorRepository.CreateSponsorAsync(Arg.Any<CreateSponsorInputDataModel>()).Returns(1);

        var result = await _sponsorService.CreateSponsorAsync(CreateInput());

        result.IsSuccess.Should().BeTrue();
        result.AnyAffectedRows.Should().BeTrue();
        await _sponsorRepository.Received(1).CreateSponsorAsync(
            Arg.Is<CreateSponsorInputDataModel>(model => model.SendAlsoTos.Count == 0));
    }

    [Fact]
    public async Task CreateSponsorAsync_Persists_Normalized_Recipients()
    {
        _sponsorRepository.CreateSponsorAsync(Arg.Any<CreateSponsorInputDataModel>()).Returns(2);

        var result = await _sponsorService.CreateSponsorAsync(CreateInput(
        [
            new CreateSendAlsoToInputModel("  luIs pérez ", "  LuIs@Ejemplo.COM ", true),
        ]));

        result.IsSuccess.Should().BeTrue();
        await _sponsorRepository.Received(1).CreateSponsorAsync(
            Arg.Is<CreateSponsorInputDataModel>(model =>
                model.SendAlsoTos.Count == 1
                && model.SendAlsoTos[0].RecipientName == "Luis Pérez"
                && model.SendAlsoTos[0].RecipientEmail == "luis@ejemplo.com"
                && model.SendAlsoTos[0].SendAsBcc));
    }

    [Fact]
    public async Task CreateSponsorAsync_Rejects_Duplicate_And_Primary_Emails()
    {
        var duplicate = await _sponsorService.CreateSponsorAsync(CreateInput(
        [
            new CreateSendAlsoToInputModel("Luis Perez", "luis@ejemplo.com", false),
            new CreateSendAlsoToInputModel("Ana Gomez", " LUIS@ejemplo.com ", true),
        ]));

        var matchesPrimary = await _sponsorService.CreateSponsorAsync(CreateInput(
        [
            new CreateSendAlsoToInputModel("Luis Perez", " JANE@x.com ", false),
        ]));

        duplicate.IsSuccess.Should().BeFalse();
        duplicate.Errors.Should().Contain(SendAlsoToValidator.DuplicateMessage);
        matchesPrimary.Errors.Should().Contain(SendAlsoToValidator.MatchesPrimaryMessage);
        await _sponsorRepository.DidNotReceive().CreateSponsorAsync(Arg.Any<CreateSponsorInputDataModel>());
    }

    [Fact]
    public async Task UpdateSponsorSendAlsoTosAsync_Rejects_Sponsor_Outside_Chapter()
    {
        _sponsorRepository.GetSendAlsoTosBySponsorIdAsync(5, 2)
            .Returns((SponsorSendAlsoTosDataModel?)null);

        var result = await _sponsorService.UpdateSponsorSendAlsoTosAsync(UpdateInput(chapterId: 2));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(SendAlsoToValidator.NotAvailableMessage);
        await _sponsorRepository.DidNotReceive()
            .UpdateSendAlsoTosAsync(Arg.Any<UpdateSponsorSendAlsoTosInputDataModel>());
    }

    [Fact]
    public async Task UpdateSponsorSendAlsoTosAsync_Rejects_Inactive_Sponsor()
    {
        _sponsorRepository.GetSendAlsoTosBySponsorIdAsync(5, 2)
            .Returns(new SponsorSendAlsoTosDataModel
            {
                SponsorId = 5,
                SponsorFirstName = "Ana",
                SponsorLastName = "Perez",
                SponsorEmail = "ana@ejemplo.com",
                IsSponsorActive = false,
            });

        var result = await _sponsorService.UpdateSponsorSendAlsoTosAsync(UpdateInput(chapterId: 2));

        result.Errors.Should().Contain(SendAlsoToValidator.InactiveMessage);
        await _sponsorRepository.DidNotReceive()
            .UpdateSendAlsoTosAsync(Arg.Any<UpdateSponsorSendAlsoTosInputDataModel>());
    }

    [Fact]
    public async Task UpdateSponsorSendAlsoTosAsync_Rejects_Email_Matching_Stored_Primary()
    {
        _sponsorRepository.GetSendAlsoTosBySponsorIdAsync(5, null)
            .Returns(new SponsorSendAlsoTosDataModel
            {
                SponsorId = 5,
                SponsorFirstName = "Ana",
                SponsorLastName = "Perez",
                SponsorEmail = "ana@ejemplo.com",
                IsSponsorActive = true,
            });

        var result = await _sponsorService.UpdateSponsorSendAlsoTosAsync(
            UpdateInput(null, [new UpdateSendAlsoToInputModel(0, "Luis Perez", " ANA@ejemplo.com ", false)]));

        result.Errors.Should().Contain(SendAlsoToValidator.MatchesPrimaryMessage);
        await _sponsorRepository.DidNotReceive()
            .UpdateSendAlsoTosAsync(Arg.Any<UpdateSponsorSendAlsoTosInputDataModel>());
    }

    [Fact]
    public async Task UpdateSponsorSendAlsoTosAsync_Updates_When_Recipients_Are_Valid()
    {
        _sponsorRepository.GetSendAlsoTosBySponsorIdAsync(5, 2)
            .Returns(new SponsorSendAlsoTosDataModel
            {
                SponsorId = 5,
                SponsorFirstName = "Ana",
                SponsorLastName = "Perez",
                SponsorEmail = "ana@ejemplo.com",
                IsSponsorActive = true,
            });
        _sponsorRepository.UpdateSendAlsoTosAsync(Arg.Any<UpdateSponsorSendAlsoTosInputDataModel>())
            .Returns(new UpdateSendAlsoTosRepositoryResult(SponsorFound: true, AffectedRows: 1));

        var result = await _sponsorService.UpdateSponsorSendAlsoTosAsync(UpdateInput(
            2,
            [new UpdateSendAlsoToInputModel(0, "Luis Perez", "luis@ejemplo.com", true)]));

        result.IsSuccess.Should().BeTrue();
        result.AnyAffectedRows.Should().BeTrue();
        await _sponsorRepository.Received(1).UpdateSendAlsoTosAsync(
            Arg.Is<UpdateSponsorSendAlsoTosInputDataModel>(model =>
                model.SponsorId == 5
                && model.ChapterId == 2
                && model.Recipients.Count == 1
                && model.Recipients[0].RecipientEmail == "luis@ejemplo.com"
                && model.Recipients[0].SendAsBcc));
    }

    private static CreateSponsorInputModel CreateInput(List<CreateSendAlsoToInputModel>? recipients = null) =>
        new(
            ChapterId: 1,
            SponsorFirstName: "Jane",
            SponsorLastName: "Smith",
            SponsorNickName: "JS",
            SponsorGender: Gender.Female,
            SponsorEmail: "jane@x.com",
            SponsorPhoneNumber: "555-1234",
            SponsorCompanyId: null,
            SponsorNotes: "Notes",
            CreatedById: 9,
            SendAlsoTos: recipients);

    private static UpdateSponsorSendAlsoTosInputModel UpdateInput(
        int? chapterId,
        List<UpdateSendAlsoToInputModel>? recipients = null) =>
        new(5, chapterId, recipients ?? [], 9);
}
