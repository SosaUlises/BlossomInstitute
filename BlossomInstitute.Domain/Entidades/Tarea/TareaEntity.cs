using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Entidades.Profesor;

namespace BlossomInstitute.Domain.Entidades.Tarea
{
    public class TareaEntity
    {
        public int Id { get; set; }

        public int CursoId { get; set; }
        public CursoEntity Curso { get; set; } = default!;

        public int ProfesorId { get; set; }
        public ProfesorEntity Profesor { get; set; } = default!;

        public string Titulo { get; set; } = default!;
        public string? Consigna { get; set; }

        public DateTime? FechaEntregaUtc { get; set; } // deadline
        public EstadoTarea Estado { get; set; } = EstadoTarea.Borrador;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        public List<TareaRecursoEntity> Recursos { get; set; } = new();
    }
}
