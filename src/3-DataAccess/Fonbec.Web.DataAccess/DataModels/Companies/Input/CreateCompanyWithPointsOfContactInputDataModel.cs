namespace Fonbec.Web.DataAccess.DataModels.Companies.Input;

public class CreateCompanyWithPointsOfContactInputDataModel : CreateCompanyInputDataModel
{
    public List<CreatePointOfContactInputDataModel> PointsOfContact { get; set; } = [];
}