using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fonbec.Web.DataAccess.DataModels.Chapters;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.ViewModels.Chapters;
using Mapster;

namespace Fonbec.Web.Logic.Services
{
    public interface IEditChapterService
    {
        Task<EditChapterInputModel> EditChapterAsync(EditChapterInputModel chapter);
    }
    public class EditChapterService(IEditChapterRepository editChapterRepository) : IEditChapterService
    {
        public async Task<EditChapterInputModel> EditChapterAsync(EditChapterInputModel chapter)
        {
            var dataModel = chapter.Adapt<EditChapterInputDataModel>();
            var result = await editChapterRepository.EditChapterAsync(dataModel);

            return result.Adapt<EditChapterInputModel>();

        }
    }
}
