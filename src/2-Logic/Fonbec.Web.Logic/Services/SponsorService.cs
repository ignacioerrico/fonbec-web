using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Models.Results;
using Fonbec.Web.Logic.Models.Sponsors;
using Fonbec.Web.Logic.Models.Sponsors.Input;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface ISponsorService
{
    Task<List<SponsorsListViewModel>> GetAllSponsorsAsync(int? chapterId);
    Task<List<SelectableModel<int>>> GetAllSponsorsForSelectionAsync(int? chapterId);
    Task<CrudResult> CreateSponsorAsync(CreateSponsorInputModel createSponsorInputModel);
    Task<CrudResult> UpdateSponsorAsync(UpdateSponsorInputModel inputModel);
    Task<SponsorRecipientsViewModel?> GetSponsorRecipientsAsync(int sponsorId, int? chapterId);
    Task<CrudResult> UpdateSponsorSendAlsoTosAsync(UpdateSponsorSendAlsoTosInputModel inputModel);
}

public class SponsorService(ISponsorRepository sponsorRepository) : ISponsorService
{
    public async Task<List<SponsorsListViewModel>> GetAllSponsorsAsync(int? chapterId)
    {
        var allSponsorsDataModel = await sponsorRepository.GetAllSponsorsAsync(chapterId);
        var allSponsorsListViewModel = allSponsorsDataModel.Adapt<List<SponsorsListViewModel>>();
        return allSponsorsListViewModel;
    }

    public async Task<List<SelectableModel<int>>> GetAllSponsorsForSelectionAsync(int? chapterId)
    {
        var sponsors = await GetAllSponsorsAsync(chapterId);
        return sponsors.Adapt<List<SelectableModel<int>>>();
    }

    public async Task<CrudResult> CreateSponsorAsync(CreateSponsorInputModel inputModel)
    {
        var errors = SendAlsoToValidator.Validate(
            (inputModel.SendAlsoTos ?? []).Select(recipient =>
                new SendAlsoToValidationItem(
                    recipient.RecipientName,
                    recipient.RecipientEmail)),
            inputModel.SponsorEmail);

        if (errors.Count > 0)
        {
            return new CrudResult(Errors: errors);
        }

        var inputDataModel = inputModel.Adapt<CreateSponsorInputDataModel>();
        var affectedRows = await sponsorRepository.CreateSponsorAsync(inputDataModel);
        return new CrudResult(affectedRows);
    }

    public async Task<CrudResult> UpdateSponsorAsync(UpdateSponsorInputModel inputModel)
    {
        var dataModel = inputModel.Adapt<UpdateSponsorInputDataModel>();
        var affectedRows = await sponsorRepository.UpdateSponsorAsync(dataModel);
        return new CrudResult(affectedRows);
    }

    public async Task<SponsorRecipientsViewModel?> GetSponsorRecipientsAsync(int sponsorId, int? chapterId)
    {
        var dataModel = await sponsorRepository.GetSendAlsoTosBySponsorIdAsync(sponsorId, chapterId);
        return dataModel?.Adapt<SponsorRecipientsViewModel>();
    }

    public async Task<CrudResult> UpdateSponsorSendAlsoTosAsync(UpdateSponsorSendAlsoTosInputModel inputModel)
    {
        var sponsor = await sponsorRepository.GetSendAlsoTosBySponsorIdAsync(inputModel.SponsorId, inputModel.ChapterId);
        if (sponsor is null)
        {
            return new CrudResult(Errors: [SendAlsoToValidator.NotAvailableMessage]);
        }

        if (!sponsor.IsSponsorActive)
        {
            return new CrudResult(Errors: [SendAlsoToValidator.InactiveMessage]);
        }

        var errors = SendAlsoToValidator.Validate(
            inputModel.Recipients.Select(recipient =>
                new SendAlsoToValidationItem(
                    recipient.RecipientName,
                    recipient.RecipientEmail)),
            sponsor.SponsorEmail);

        if (errors.Count > 0)
        {
            return new CrudResult(Errors: errors);
        }

        var dataModel = inputModel.Adapt<UpdateSponsorSendAlsoTosInputDataModel>();
        var result = await sponsorRepository.UpdateSendAlsoTosAsync(dataModel);
        if (!result.SponsorFound)
        {
            return new CrudResult(Errors: [SendAlsoToValidator.NotAvailableMessage]);
        }

        if (result.Rejected)
        {
            return new CrudResult(Errors: [SendAlsoToValidator.SaveFailedMessage]);
        }

        return new CrudResult(result.AffectedRows);
    }
}