using BlossomInstitute.Domain.Entidades.Entrega;

namespace BlossomInstitute.Application.DataBase.Dashboard.Queries.ProfesoresModels
{
    public class ProfesorDashboardUltimaEntregaItemModel
    {
        public int EntregaId { get; set; }
        public int TareaId { get; set; }
        public int CursoId { get; set; }
        public string CursoNombre { get; set; } = default!;
        public string TituloTarea { get; set; } = default!;
        public int AlumnoId { get; set; }
        public string AlumnoNombre { get; set; } = default!;
        public string AlumnoApellido { get; set; } = default!;
        public string? AlumnoAvatarUrl { get; set; }
        public DateTime FechaEntregaUtc { get; set; }
        public EstadoEntrega EstadoEntrega { get; set; }
        public bool TieneFeedbackVigente { get; set; }
    }
}
