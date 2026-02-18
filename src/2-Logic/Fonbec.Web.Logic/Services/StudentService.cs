using Fonbec.Web.DataAccess.DataModels.Students;
using Fonbec.Web.DataAccess.DataModels.Students.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Results;
using Fonbec.Web.Logic.Models.Students;
using Fonbec.Web.Logic.Models.Students.Input;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface IStudentService
{
    Task<List<StudentsListViewModel>> GetAllStudentsAsync();
    Task<List<StudentsListViewModel>> GetStudentsAsync(StudentsFilterModel filter);
    Task<CrudResult> CreateStudentAsync(CreateStudentInputModel inputModel);
    Task<CrudResult> UpdateStudentAsync(UpdateStudentInputModel inputModel);
}

public class StudentService(IStudentRepository studentRepository) : IStudentService
{
    public async Task<List<StudentsListViewModel>> GetAllStudentsAsync()
    {
        var allStudentsDataModel = await studentRepository.GetAllStudentsAsync();
        var studentsListViewModel = allStudentsDataModel.Adapt<List<StudentsListViewModel>>();
        return studentsListViewModel;
    }

    public async Task<List<StudentsListViewModel>> GetStudentsAsync(StudentsFilterModel filter)
    {
        try
        {
            filter ??= new StudentsFilterModel();
            filter.FacilitatorIds ??= new List<int>();
            var data = await studentRepository.GetStudentsAsync(filter);
            if (data == null || !data.Any())
                return new List<StudentsListViewModel>();

            return data.Adapt<List<StudentsListViewModel>>();
        }
        catch (Exception ex)
        {
            return new List<StudentsListViewModel>();
        }
    }
    public async Task<CrudResult> CreateStudentAsync(CreateStudentInputModel inputModel)
    {
        var createStudentInputDataModel = inputModel.Adapt<CreateStudentInputDataModel>();
        var affectedRows = await studentRepository.CreateStudentAsync(createStudentInputDataModel);
        return new CrudResult(affectedRows);
    }

    public async Task<CrudResult> UpdateStudentAsync(UpdateStudentInputModel inputModel)
    {
        var updateStudentInputDataModel = inputModel.Adapt<UpdateStudentInputDataModel>();
        var affectedRows = await studentRepository.UpdateStudentAsync(updateStudentInputDataModel);
        return new CrudResult(affectedRows);
    }
}