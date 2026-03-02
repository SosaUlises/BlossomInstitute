using BlossomInstitute.Domain.Entidades.Tarea;

namespace BlossomInstitute.Application.DataBase.Tarea.Commands.UpdateTarea
{
    public class UpdateTareaRecursoModel
    {
        public TipoRecursoTarea Tipo { get; set; }
        public string Url { get; set; } = default!;
        public string? Nombre { get; set; }
    }
}
