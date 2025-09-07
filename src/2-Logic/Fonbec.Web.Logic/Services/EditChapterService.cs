using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.ViewModels.Chapters;
using Mapster;

namespace Fonbec.Web.Logic.Services
{
    public interface IEditChapterService
    {
        Task<EditChapterViewModel> EditChapterAsync(Chapter chapter);
    }
    public class EditChapterService(IEditChapterRepository editChapterRepository) : IEditChapterService
    {
        public async Task<EditChapterViewModel> EditChapterAsync(Chapter chapter)
        {
            var result = await editChapterRepository.EditChapterAsync(chapter);
            var viewModel = result.Adapt<EditChapterViewModel>();

            return viewModel;
        }
    }
}
