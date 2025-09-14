using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Logic.ViewModels.Chapters;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Chapters
{
    public partial class EditChapter
    {
        private MudForm _form;
        private string _nombre = string.Empty;
        private int _id;

        [Parameter]
        public EditChapterInputModel? ChapterToEdit { get; set; }
        [Parameter]
        public EventCallback<EditChapterInputModel> OnChapterEdited { get; set; } 

        [Inject]
        public IEditChapterService EditChapterService { get; set; } = null!;

        private async Task OnChapterChange()
        {
            if (ChapterToEdit != null)
            {
                _nombre = ChapterToEdit.ChapterName;
                _id = ChapterToEdit.ChapterID;
            }
        }

        private async Task EditAChapter()
        {
            if (_form.IsValid) //Si no esta vacío
            {
                var chapter = new EditChapterInputModel { ChapterName = _nombre, ChapterID = ChapterToEdit.ChapterID };
                var newChapter = await EditChapterService.EditChapterAsync(chapter);
                await OnChapterEdited.InvokeAsync();
                _nombre = string.Empty;
            }
        }

        private void Cancel()
        {
            _nombre = string.Empty;
        }
    }
}
