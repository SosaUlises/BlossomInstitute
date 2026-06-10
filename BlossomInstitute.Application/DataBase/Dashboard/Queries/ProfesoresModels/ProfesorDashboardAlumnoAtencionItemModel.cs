using BlossomInstitute.Domain.Entidades.Calificacion;

namespace BlossomInstitute.Application.DataBase.Dashboard.Queries.ProfesoresModels
{
    public class ProfesorDashboardAlumnoAtencionItemModel
    {
        public int AlumnoId { get; set; }
        public string AlumnoNombre { get; set; } = default!;
        public string? AlumnoAvatarUrl { get; set; }
        public int CursoId { get; set; }
        public string CursoNombre { get; set; } = default!;
        public string PeriodoLabel { get; set; } = default!;
        public decimal? Promedio { get; set; }
        public decimal? Asistencia { get; set; }
        public string? CalificacionBajaTitulo { get; set; }
        public TipoCalificacion? CalificacionBajaTipo { get; set; }
        public decimal? CalificacionBajaNota { get; set; }
        public string Severidad { get; set; } = "attention";
    }
}
