using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fonbec.Web.DataAccess.DataModels.Chapters
{
    public class EditChapterInputDataModel
    {
        public string ChapterName { get; init; } = string.Empty;
        public int ChapterID { get; init; }
    }
}
