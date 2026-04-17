using BlossomInstitute.Domain.Entidades.Calificacion;

namespace BlossomInstitute.Application.DataBase.Dashboard.Queries.GetAdminDashboard
{

        public class GetAdminDashboardResponseModel
        {
            public DashboardOverviewModel Overview { get; set; } = new();
            public decimal? GeneralAverage { get; set; }

            public List<DashboardAverageGradeByCourseModel> AverageGradesByCourse { get; set; } = new();
            public List<DashboardAverageGradeByCourseModel> ManualAverageGradesByCourse { get; set; } = new();

            public int StudentsAtRiskThisMonthCount { get; set; }
            public int StudentsManualLowGradesThisMonthCount { get; set; }

            public List<DashboardLowManualGradeAlertModel> StudentsManualLowPerformance { get; set; } = [];
           public List<DashboardAverageGradeByCourseModel> CoursesAtRiskByOverallAverage { get; set; } = new();
            public List<DashboardAverageGradeByCourseModel> CoursesAtRiskByManualAverage { get; set; } = new();

            public List<DashboardUpcomingAssignmentModel> UpcomingAssignments { get; set; } = new();
            public List<DashboardUpcomingClassModel> UpcomingClasses { get; set; } = new();
        }

        public class DashboardOverviewModel
        {
            public int StudentsCount { get; set; }
            public int TeachersCount { get; set; }
            public int ActiveCoursesCount { get; set; }
            public int PendingAssignmentsCount { get; set; }
        }

        public class DashboardAverageGradeByCourseModel
        {
            public int CursoId { get; set; }
            public string CursoNombre { get; set; } = default!;
            public decimal AverageGrade { get; set; }
        }

        public class DashboardUpcomingAssignmentModel
        {
            public int TareaId { get; set; }
            public string Titulo { get; set; } = default!;
            public int CursoId { get; set; }
            public string CursoNombre { get; set; } = default!;
            public DateTime FechaEntregaUtc { get; set; }
        }

        public class DashboardUpcomingClassModel
        {
            public int CursoId { get; set; }
            public string CursoNombre { get; set; } = default!;
            public string ProfesorNombre { get; set; } = default!;
            public string DiaSemana { get; set; } = default!;
            public TimeOnly HoraInicio { get; set; }
            public DateTime ProximaClase { get; set; }
        }

        public class DashboardLowPerformanceStudentModel
        {
            public int AlumnoId { get; set; }
            public string AlumnoNombre { get; set; } = default!;
            public decimal LowestGrade { get; set; }
            public decimal AverageGrade { get; set; }
            public int CalificacionesCount { get; set; }
            public int LowGradesCount { get; set; }
            public string? CursoNombre { get; set; }
        }

    public class DashboardLowManualGradeAlertModel
    {
        public int AlumnoId { get; set; }
        public string AlumnoNombre { get; set; } = default!;
        public int CursoId { get; set; }
        public string CursoNombre { get; set; } = default!;
        public int CalificacionId { get; set; }
        public string Titulo { get; set; } = default!;
        public TipoCalificacion Tipo { get; set; }
        public decimal Nota { get; set; }
        public DateOnly Fecha { get; set; }
    }
}