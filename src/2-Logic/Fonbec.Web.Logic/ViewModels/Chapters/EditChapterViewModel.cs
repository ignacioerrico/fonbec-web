using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fonbec.Web.DataAccess.DataModels.Chapters;
using Mapster;

namespace Fonbec.Web.Logic.ViewModels.Chapters
{
    public class EditChapterViewModel
    {
        public string ChapterName { get; init; } = string.Empty;
        public string ChapterID { get; init; } = string.Empty;
    }

    public class EditChapterViewModelMappingDefinitions : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<ChaptersListDataModel, ChaptersListViewModel>()
                .Map(dest => dest.ChapterName, src => src.ChapterName);
        }
    }
}
