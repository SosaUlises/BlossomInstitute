namespace BlossomInstitute.Application.DataBase.Dashboard.Queries.ProfesoresModels
{
    public class ProfesorDashboardResponseModel
    {
        public int ProfesorId { get; set; }
        public string Nombre { get; set; } = default!;
        public string Apellido { get; set; } = default!;
        public long Dni { get; set; }
        public string? Email { get; set; }

        public int CantidadCursos { get; set; }
        public int CantidadAlumnos { get; set; }
        public int TareasPublicadasCount { get; set; }
        public int EntregasPendientesCorreccionCount { get; set; }

        public List<ProfesorDashboardCursoItemModel> Cursos { get; set; } = new();
        public List<ProfesorDashboardProximaClaseItemModel> ProximasClases { get; set; } = new();
        public List<ProfesorDashboardUltimaClaseItemModel> UltimasClases { get; set; } = new();
        public List<ProfesorDashboardUltimaEntregaItemModel> UltimasEntregas { get; set; } = new();
        public List<ProfesorDashboardResumenCursoItemModel> ResumenPorCurso { get; set; } = new();
        public List<ProfesorDashboardAlumnoAtencionItemModel> AlumnosQueRequierenAtencion { get; set; } = new();
    }
}
