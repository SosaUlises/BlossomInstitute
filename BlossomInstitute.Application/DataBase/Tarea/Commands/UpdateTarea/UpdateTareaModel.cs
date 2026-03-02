using BlossomInstitute.Domain.Entidades.Tarea;

namespace BlossomInstitute.Application.DataBase.Tarea.Commands.UpdateTarea
{
    public class UpdateTareaModel
    {
        public string Titulo { get; set; } = default!;
        public string? Consigna { get; set; }
        public DateTime? FechaEntregaUtc { get; set; }
        public EstadoTarea Estado { get; set; }

        // Para simplificar: reemplazamos recursos completos (delete + add).
        public List<UpdateTareaRecursoModel> Recursos { get; set; } = new();
    }
}
