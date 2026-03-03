using BlossomInstitute.Domain.Entidades.Alumno;
using BlossomInstitute.Domain.Entidades.Tarea;

namespace BlossomInstitute.Domain.Entidades.Entrega
{
    public class EntregaEntity
    {
        public int Id { get; set; }

        public int TareaId { get; set; }
        public TareaEntity Tarea { get; set; } = default!;

        public int AlumnoId { get; set; }
        public AlumnoEntity Alumno { get; set; } = default!;

        public string? Texto { get; set; }

        public DateTime FechaEntregaUtc { get; set; }
        public EstadoEntrega Estado { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        public List<EntregaAdjuntoEntity> Adjuntos { get; set; } = new();

        public List<FeedbackEntregaEntity> Feedbacks { get; set; } = new();
    }
}
