using BlossomInstitute.Application.Common.Academico;
using BlossomInstitute.Domain.Entidades.Calificacion;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Entidades.Entrega;

namespace BlossomInstitute.Application.DataBase.Alumno.Queries.GetAcademicSummary
{
    internal sealed class MatriculaAlumnoProjection
    {
        public int CursoId { get; init; }
        public string NombreCurso { get; init; } = default!;
        public string? DescripcionCurso { get; init; }
        public EstadoCurso EstadoCurso { get; init; }
    }

    internal sealed class ProfesorCursoProjection
    {
        public int CursoId { get; init; }
        public string Nombre { get; init; } = default!;
        public string Apellido { get; init; } = default!;
        public string? AvatarUrl { get; init; }
    }

    internal sealed class ProfesorResumenProjection
    {
        public string NombreCompleto { get; init; } = default!;
        public string? AvatarUrl { get; init; }
    }

    internal sealed class ClaseAlumnoProjection
    {
        public int Id { get; init; }
        public int CursoId { get; init; }
        public DateOnly Fecha { get; init; }
    }

    internal sealed class AsistenciaAlumnoProjection
    {
        public int ClaseId { get; init; }
        public DateOnly Fecha { get; init; }
        public EstadoAsistencia Estado { get; init; }
    }

    internal sealed class CalificacionAlumnoProjection
    {
        public int Id { get; init; }
        public int CursoId { get; init; }
        public string NombreCurso { get; init; } = default!;
        public string Titulo { get; init; } = default!;
        public TipoCalificacion Tipo { get; init; }
        public decimal Nota { get; init; }
        public DateOnly Fecha { get; init; }
    }

    internal sealed class CorreccionEntregaProjection
    {
        public int EntregaId { get; init; }
        public EstadoCorreccion Estado { get; init; }
    }

    internal readonly record struct SeguimientoPendienteKey(
        int CursoId,
        int Anio,
        int NumeroTrimestre);

    internal sealed class SeguimientoPendienteAcumulador
    {
        public int CursoId { get; init; }
        public string NombreCurso { get; init; } = default!;
        public string EtiquetaPeriodo { get; init; } = default!;
        public int NumeroTrimestre { get; init; }
        public int Anio { get; init; }
        public decimal? Promedio { get; set; }
        public decimal? Asistencia { get; set; }
        public bool EsTrimestreAnterior { get; init; }
        public List<string> Motivos { get; } = new();

        public AlumnoAcademicPendingFollowUpModel ToModel()
        {
            var esCritico =
                (Promedio.HasValue && Promedio.Value < ReglasAcademicas.UmbralNotaBaja) ||
                (Asistencia.HasValue && Asistencia.Value < ReglasAcademicas.UmbralAsistenciaBaja);
            var nivel = esCritico ? "critical" : "follow-up";
            var motivo = string.Join(" y ", Motivos.Distinct());
            var motivoPrincipal = Motivos.Distinct().FirstOrDefault() ?? "Seguimiento pendiente";
            var detalleValor = Promedio.HasValue
                ? $"Promedio {Promedio:0.##}"
                : Asistencia.HasValue
                    ? $"Asistencia {Asistencia:0.##}%"
                    : motivoPrincipal;

            return new AlumnoAcademicPendingFollowUpModel
            {
                CourseId = CursoId,
                CourseName = NombreCurso,
                PeriodLabel = EtiquetaPeriodo,
                QuarterNumber = NumeroTrimestre,
                Year = Anio,
                Level = nivel,
                Title = EsTrimestreAnterior
                    ? "Seguimiento pendiente del trimestre anterior"
                    : $"{(esCritico ? "Critico" : "Seguimiento")} en {EtiquetaPeriodo}",
                Reason = motivo,
                AverageValue = Promedio,
                AttendanceValue = Asistencia,
                Description = $"{detalleValor} en {EtiquetaPeriodo}",
                Status = "pending",
                IsPreviousQuarter = EsTrimestreAnterior
            };
        }
    }
}
