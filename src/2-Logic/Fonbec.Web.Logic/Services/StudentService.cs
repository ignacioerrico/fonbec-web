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