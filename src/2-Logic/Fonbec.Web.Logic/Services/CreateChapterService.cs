using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.ViewModels.Chapters;
using Mapster;

namespace Fonbec.Web.Logic.Services
{
    public interface ICreateChapterService
    {
        Task<CreateChapterViewModel> CreateChapterAsync(Chapter chapter);
    }
    public class CreateChapterService(ICreateChapterRepository createChapterRepository) : ICreateChapterService
    {
        public async Task<CreateChapterViewModel> CreateChapterAsync(Chapter chapter)
        {
            var result = await createChapterRepository.CreateChapterAsync(chapter);
            var viewModel = result.Adapt<CreateChapterViewModel>();

            return viewModel;
        }
    }
}
