using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Models.Companies;
using Fonbec.Web.Logic.Models.Companies.Input;
using Fonbec.Web.Logic.Models.Results;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface ICompanyService
{
    Task<List<CompaniesListViewModel>> GetAllCompaniesAsync();
    Task<List<SelectableModel<int>>> GetAllCompaniesForSelectionAsync();
    Task<bool> CompanyNameExistsAsync(string companyName);
    Task<CrudResult> CreateCompanyAsync(CreateCompanyInputModel inputModel);
}

public class CompanyService(ICompanyRepository companyRepository, ISponsorRepository sponsorRepository) : ICompanyService
{
    public async Task<List<CompaniesListViewModel>> GetAllCompaniesAsync()
    {
        var allCompaniesDataModel = await companyRepository.GetAllCompaniesAsync();
        var allCompaniesListViewModel = allCompaniesDataModel.Adapt<List<CompaniesListViewModel>>();
        return allCompaniesListViewModel;
    }

    public async Task<List<SelectableModel<int>>> GetAllCompaniesForSelectionAsync()
    {
        return await GetAllCompaniesAsync()
            .ContinueWith(t => t.Result.Adapt<List<SelectableModel<int>>>());
    }

    public async Task<bool> CompanyNameExistsAsync(string companyName)
    {
        var normalizedCompanyName = companyName.NormalizeText();
        return await companyRepository.CompanyNameExistsAsync(normalizedCompanyName);
    }

    public async Task<CrudResult> CreateCompanyAsync(CreateCompanyInputModel inputModel)
    {
        var inputDataModel = inputModel.Adapt<CreateCompanyInputDataModel>();

        var companyId = await companyRepository.CreateCompanyAsync(inputDataModel);

        if (companyId == 0)
        {
            return new CrudResult();
        }

        int affectedRows = 0;

        if (inputDataModel.SponsorIds.Count > 0)
        {
            affectedRows = await sponsorRepository.LinkSponsorsToCompanyAsync(inputDataModel.SponsorIds, companyId);
        }

        return new CrudResult(affectedRows);
    }
}