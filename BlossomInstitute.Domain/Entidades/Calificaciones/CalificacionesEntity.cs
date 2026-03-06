using BlossomInstitute.Domain.Entidades.Alumno;
using BlossomInstitute.Domain.Entidades.Curso;

namespace BlossomInstitute.Domain.Entidades.Calificaciones
{
    public class CalificacionEntity
    {
        public int Id { get; set; }

        public int CursoId { get; set; }
        public CursoEntity Curso { get; set; } = default!;

        public int AlumnoId { get; set; }
        public AlumnoEntity Alumno { get; set; } = default!;

        public string NombreEvaluacion { get; set; } = default!;
        public decimal Nota { get; set; }
        public DateOnly Fecha { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        public bool Archivada { get; set; }
    }
}
