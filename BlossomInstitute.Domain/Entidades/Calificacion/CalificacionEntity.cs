using BlossomInstitute.Domain.Entidades.Alumno;
using BlossomInstitute.Domain.Entidades.Calificacion;
using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Entidades.Entrega;
using BlossomInstitute.Domain.Entidades.Tarea;

namespace BlossomInstitute.Domain.Entidades.Calificaciones
{
    public class CalificacionEntity
    {
        public int Id { get; set; }

        public int CursoId { get; set; }
        public CursoEntity Curso { get; set; } = default!;

        public int AlumnoId { get; set; }
        public AlumnoEntity Alumno { get; set; } = default!;

        public TipoCalificacion Tipo { get; set; }
        public string Titulo { get; set; } = default!;
        public string? Descripcion { get; set; }

        public decimal Nota { get; set; }
        public DateOnly Fecha { get; set; }

        public int? TareaId { get; set; }
        public TareaEntity? Tarea { get; set; }

        public int? EntregaId { get; set; }
        public EntregaEntity? Entrega { get; set; }

        public bool Archivado { get; set; } = false;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
