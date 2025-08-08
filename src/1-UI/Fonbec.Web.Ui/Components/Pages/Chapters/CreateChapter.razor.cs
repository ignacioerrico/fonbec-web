using MudBlazor;
using Fonbec.Web.Logic.ViewModels.Chapters;
using Microsoft.AspNetCore.Components;
using Fonbec.Web.Logic.Services;
using System.Threading.Tasks;
using Fonbec.Web.DataAccess.Entities;

namespace Fonbec.Web.Ui.Components.Pages.Chapters
{
    public partial class CreateChapter
    {
        private MudForm _form;
        private string _nombre;

        [Inject]
        public ICreateChapterService CreateChapterService { get; set; } = null!;
        [Parameter]
        public EventCallback OnChapterCreated { get; set; }
        private async Task CreateAChapter()
        {
            if (_form.IsValid) //Si no esta vacío
            {
                var chapter = new Chapter(_nombre);
                await CreateChapterService.CreateChapterAsync(chapter);
                await OnChapterCreated.InvokeAsync(); // Notificar al ChapterList
                _nombre = string.Empty;
            }
        }

        private void Cancel()
        {
            _nombre = string.Empty;
        }
    }
}
