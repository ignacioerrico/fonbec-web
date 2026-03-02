using System;
using System.Collections.Generic;
using System.Text;
using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Models.Chapters;
using Fonbec.Web.Logic.Models.Companies;
using Fonbec.Web.Logic.Models.Companies.Input;
using Fonbec.Web.Logic.Models.Results;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface ICompanyService
{
    Task<CrudResult> CreateCompanyAsync(CreateCompanyInputModel inputModel);
    Task<bool> CompanyNameExistsAsync(string companyName);
    Task<List<SelectableModel<int>>> GetAllCompaniesForSelectionAsync();
    Task<List<CompaniesListViewModel>> GetAllCompaniesAsync();
}

public class CompanyService(ICompanyRepository companyRepository) : ICompanyService
{
    public async Task<CrudResult> CreateCompanyAsync(CreateCompanyInputModel inputModel)
    {
        var inputDataModel = inputModel.Adapt<CreateCompanyInputDataModel>();
        var affectedRows = await companyRepository.CreateCompanyAsync(inputDataModel);
        return new CrudResult(affectedRows);
    }

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
        return await companyRepository.CompanyNameExistsAsync(companyName);
    }

}