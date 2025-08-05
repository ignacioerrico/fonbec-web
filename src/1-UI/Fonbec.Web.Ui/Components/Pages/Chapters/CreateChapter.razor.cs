using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Chapters
{
    public partial class CreateChapter
    {
        private MudForm _form;
        private string _nombre = string.Empty;

        private void CrearFilial()
        {
            if (_form.IsValid) //Sino esta vacio
            {
                return; //Aca se llamaría al servicio para agregar el _nombre
            }
        }

        private void Cancelar()
        {
            _nombre = string.Empty;
        }
    }
}
