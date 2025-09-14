using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fonbec.Web.DataAccess.DataModels.Chapters;
using Mapster;

namespace Fonbec.Web.Logic.ViewModels.Chapters
{
    public class EditChapterInputModel
    {
        public string ChapterName { get; set; } = string.Empty;
        public int ChapterID { get; set; }
    }

    public class EditChapterInputModelMappingDefinitions : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<EditChapterInputDataModel, EditChapterInputModel>()
                .Map(dest => dest.ChapterName, src => src.ChapterName)
                .Map(dest => dest.ChapterID, src => src.ChapterID);
        }
    }
}
