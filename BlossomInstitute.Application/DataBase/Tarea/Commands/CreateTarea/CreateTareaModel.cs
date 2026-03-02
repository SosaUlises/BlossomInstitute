using BlossomInstitute.Domain.Entidades.Tarea;

namespace BlossomInstitute.Application.DataBase.Tarea.Commands.CreateTarea
{
    public class CreateTareaModel
    {
        public string Titulo { get; set; } = default!;
        public string? Consigna { get; set; }
        public DateTime? FechaEntregaUtc { get; set; }
        public EstadoTarea Estado { get; set; } = EstadoTarea.Publicada;

        public List<CreateTareaRecursoModel> Recursos { get; set; } = new();
    }
}
