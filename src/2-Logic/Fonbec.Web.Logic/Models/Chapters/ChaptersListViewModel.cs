using Fonbec.Web.DataAccess.DataModels;
using Fonbec.Web.DataAccess.DataModels.Chapters;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Chapters;

public class ChaptersListViewModel : AuditableViewModel, IDetectChanges<ChaptersListViewModel>
{
    public int ChapterId { get; set; }
    public string ChapterName { get; init; } = string.Empty;
    public bool IsChapterActive { get; set; }

    public List<ChapterStaffMemberViewModel> Managers { get; set; } = [];

    public List<ChapterStaffMemberViewModel> Facilitators { get; set; } = [];

    public List<ChapterStaffMemberViewModel> Reviewers { get; set; } = [];

    public int StudentCount { get; set; }

    public int SponsorCount { get; set; }

    public bool IsIdenticalTo(ChaptersListViewModel other) =>
        ChapterName == other.ChapterName.NormalizeText()
        && Notes == other.Notes.NullOrTrimmed();
}

public class ChapterStaffMemberViewModel
{
    public int UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; }

    public bool CanBeLockedOut { get; set; }

    public bool HasLinkedStudents { get; set; }

    public bool CanImproveImages { get; set; }
}

public class ChaptersListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AllChaptersDataModel, ChaptersListViewModel>()
            .Map(dest => dest.ChapterId, src => src.ChapterId)
            .Map(dest => dest.ChapterName, src => src.ChapterName)
            .Map(dest => dest.IsChapterActive, src => src.IsChapterActive);

        config.NewConfig<ChaptersListViewModel, SelectableModel<int>>()
            .Map(dest => dest.Key, src => src.ChapterId)
            .Map(dest => dest.DisplayName, src => src.ChapterName);
    }
}
