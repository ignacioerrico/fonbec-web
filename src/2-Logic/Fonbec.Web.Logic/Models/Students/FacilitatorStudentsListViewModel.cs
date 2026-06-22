using Fonbec.Web.DataAccess.DataModels.Students;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Students;

public class FacilitatorStudentsListViewModel : AuditableViewModel, IDetectChanges<FacilitatorStudentsListViewModel>
{
    public int StudentId { get; set; }

    public string StudentFirstName { get; set; } = null!;

    public string StudentLastName { get; set; } = null!;

    public bool IsIdenticalTo(FacilitatorStudentsListViewModel other) =>
        StudentFirstName == other.StudentFirstName.NormalizeText()
        && StudentLastName == other.StudentLastName.NormalizeText();
}

public class SponsorStudentsListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<FacilitatorStudentsDataModel, FacilitatorStudentsListViewModel>()
            .Map(dest => dest.StudentId, src => src.StudentId)
            .Map(dest => dest.StudentFirstName, src => src.StudentFirstName)
            .Map(dest => dest.StudentLastName, src => src.StudentLastName);
    }
}

