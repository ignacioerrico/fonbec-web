using System;
using System.Collections.Generic;
using System.Text;
using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Companies.Input;
using Fonbec.Web.Logic.Models.Results;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface ICompanyService
{
    Task<CrudResult> CreateCompanyAsync(CreateCompanyInputModel inputModel);
    Task<string?> GetCompanyByNameAsync(string companyName);
}

public class CompanyService(ICompanyRepository companyRepository) : ICompanyService
{
    public async Task<CrudResult> CreateCompanyAsync(CreateCompanyInputModel inputModel)
    {
        var inputDataModel = inputModel.Adapt<CreateCompanyInputDataModel>();
        var affectedRows = await companyRepository.CreateCompanyAsync(inputDataModel);
        return new CrudResult(affectedRows);
    }

    public async Task<string?> GetCompanyByNameAsync(string companyName)
    {
        return await companyRepository.GetCompanyByNameAsync(companyName);
    }
}