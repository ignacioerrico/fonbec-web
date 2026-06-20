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
    Task<bool> CompanyNameExistsAsync(string companyName, int? excludeCompanyId = null);
    Task<CreateCompanyResult> CreateCompanyAsync(CreateCompanyInputModel inputModel);

    Task<CrudResult> UpdateCompanyAsync(UpdateCompanyInputModel inputModel);
}

public class CompanyService(ICompanyRepository companyRepository) : ICompanyService
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

    public async Task<bool> CompanyNameExistsAsync(string companyName, int? excludeCompanyId = null)
    {
        var normalizedCompanyName = companyName.NormalizeText();
        return await companyRepository.CompanyNameExistsAsync(normalizedCompanyName, excludeCompanyId);
    }

    public async Task<CreateCompanyResult> CreateCompanyAsync(CreateCompanyInputModel inputModel)
    {
        var inputDataModel = inputModel.Adapt<CreateCompanyInputDataModel>();

        var result = await companyRepository.CreateCompanyAsync(inputDataModel);

        if (result.MissingSponsorIds is { Count: > 0 })
        {
            var sponsorsById = inputModel.Sponsors
                .GroupBy(s => s.Key)
                .ToDictionary(g => g.Key, g => g.First());
            var missingSponsors = result.MissingSponsorIds
                .Select(id => new MissingSponsor(
                    id,
                    sponsorsById.TryGetValue(id, out var sponsor) && !string.IsNullOrWhiteSpace(sponsor.DisplayName)
                        ? sponsor.DisplayName
                        : "Padrino desconocido"))
                .ToList();

            return new CreateCompanyResult(MissingSponsors: missingSponsors);
        }

        return result.CompanyId == 0
            ? new CreateCompanyResult()
            : new CreateCompanyResult(1);
    }

    public async Task<CrudResult> UpdateCompanyAsync(UpdateCompanyInputModel inputModel)
    {
        var updateCompanyInputDataModel = inputModel.Adapt<UpdateCompanyInputDataModel>();

        var nameExists = await companyRepository.CompanyNameExistsAsync(
            updateCompanyInputDataModel.CompanyUpdatedName,
            updateCompanyInputDataModel.CompanyId);
        if (nameExists)
        {
            return new CrudResult();
        }

        var affectedRows = await companyRepository.UpdateCompanyAsync(updateCompanyInputDataModel);
        return new CrudResult(affectedRows);
    }
}