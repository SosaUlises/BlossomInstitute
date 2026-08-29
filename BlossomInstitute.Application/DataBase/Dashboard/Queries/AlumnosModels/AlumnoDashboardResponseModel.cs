namespace BlossomInstitute.Application.DataBase.Dashboard.Queries.AlumnosModels
{
    public class AlumnoDashboardResponseModel
    {
        public int AlumnoId { get; set; }
        public string Nombre { get; set; } = default!;
        public string Apellido { get; set; } = default!;
        public long Dni { get; set; }
        public string? Email { get; set; }

        public int CantidadCursos { get; set; }
        public int TareasPendientesCount { get; set; }
        public int EntregasRealizadasCount { get; set; }

        public int FeedbacksRehacerCount { get; set; }
        public int FeedbacksPendientesAccionCount { get; set; }

        public decimal? PromedioGeneral { get; set; }
        public decimal PorcentajeAsistenciaGeneral { get; set; }

        public List<DashboardCursoItemModel> Cursos { get; set; } = new();
        public List<DashboardProximaClaseItemModel> ProximasClases { get; set; } = new();
        public List<DashboardUltimaClaseItemModel> UltimasClases { get; set; } = new();
        public List<DashboardTareaPendienteItemModel> TareasPendientes { get; set; } = new();
        public List<DashboardUltimaEntregaItemModel> UltimasEntregas { get; set; } = new();
        public List<DashboardUltimaCalificacionItemModel> UltimasCalificaciones { get; set; } = new();
        public List<DashboardFeedbackRecienteItemModel> FeedbacksRecientes { get; set; } = new();
        public List<DashboardResumenCursoItemModel> ResumenPorCurso { get; set; } = new();
    }
}
