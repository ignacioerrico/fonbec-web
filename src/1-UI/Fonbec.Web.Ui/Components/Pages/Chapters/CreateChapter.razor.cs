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
        private async Task CrearFilial()
        {
            if (_form.IsValid) //Sino esta vacio
            {
                var chapter = new Chapter(_nombre);
                await CreateChapterService.CreateChapterAsync(chapter);
                _nombre = string.Empty;
            }
        }

        private void Cancelar()
        {
            _nombre = string.Empty;
        }
    }
}
