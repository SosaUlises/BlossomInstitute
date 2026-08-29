using BlossomInstitute.Domain.Entidades.Clase;

namespace BlossomInstitute.Application.DataBase.Alumno.Queries.GetAll
{
    internal sealed class MatriculaAlumnoProjection
    {
        public int AlumnoId { get; init; }
        public int CursoId { get; init; }
        public string NombreCurso { get; init; } = default!;
        public string? DescripcionCurso { get; init; }
    }

    internal sealed class ClaseAlumnoProjection
    {
        public int Id { get; init; }
        public int CursoId { get; init; }
        public DateOnly Fecha { get; init; }
    }

    internal sealed class AsistenciaAlumnoProjection
    {
        public int AlumnoId { get; init; }
        public int ClaseId { get; init; }
        public EstadoAsistencia Estado { get; init; }
    }

    internal sealed class CalificacionAlumnoProjection
    {
        public int Id { get; init; }
        public int AlumnoId { get; init; }
        public int CursoId { get; init; }
        public string NombreCurso { get; init; } = default!;
        public string Titulo { get; init; } = default!;
        public decimal Nota { get; init; }
        public DateOnly Fecha { get; init; }
    }
}
