using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Chapters
{
    public partial class EditChapter
    {
        private MudForm _form;
        private string _nombre;
        private int _id;

        [Parameter]
        public Chapter? ChapterToEdit { get; set; }
        [Parameter]
        public EventCallback<Chapter> OnChapterEdited { get; set; } 

        [Inject]
        public IEditChapterService EditChapterService { get; set; } = null!;

        private async Task OnChapterChange()
        {
            if (ChapterToEdit != null)
            {
                _nombre = ChapterToEdit.Name;
                _id = ChapterToEdit.Id;
            }
        }

        private async Task EditAChapter()
        {
            if (_form.IsValid) //Si no esta vacío
            {
                var chapter = new Chapter(_nombre, _id);
                await EditChapterService.EditChapterAsync(chapter);
                await OnChapterEdited.InvokeAsync(chapter);
                _nombre = string.Empty;
            }
        }

        private void Cancel()
        {
            _nombre = string.Empty;
        }
    }
}
