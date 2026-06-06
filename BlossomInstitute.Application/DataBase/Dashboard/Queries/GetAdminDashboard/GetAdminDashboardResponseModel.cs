using BlossomInstitute.Domain.Entidades.Calificacion;

using BlossomInstitute.Application.DataBase.Curso.Shared;

namespace BlossomInstitute.Application.DataBase.Dashboard.Queries.GetAdminDashboard
{

        public class GetAdminDashboardResponseModel
        {
            public DashboardPeriodModel Period { get; set; } = new();
            public DashboardTrendComparisonModel TrendComparison { get; set; } = new();
            public DashboardRollingWindowModel ConsecutiveAbsencesWindow { get; set; } = new();
            public DashboardOverviewModel Overview { get; set; } = new();
            public decimal? GeneralAverage { get; set; }
            public decimal? CurrentPeriodAverage { get; set; }
            public decimal? InstitutionalAttendanceAverage { get; set; }
            public int InstitutionalHomeworkPendingCorrectionCount { get; set; }

            public List<DashboardAverageGradeByCourseModel> AverageGradesByCourse { get; set; } = new();
            public List<DashboardAverageGradeByCourseModel> ManualAverageGradesByCourse { get; set; } = new();

            public int StudentsAtRiskThisMonthCount { get; set; }
            public int StudentsManualLowGradesThisMonthCount { get; set; }

            public List<DashboardLowManualGradeAlertModel> StudentsManualLowPerformance { get; set; } = [];
            public List<DashboardStudentAverageRiskModel> StudentsAtRiskByAverage { get; set; } = new();
            public List<DashboardStudentAttendanceRiskModel> StudentsWithMultipleAbsences { get; set; } = new();
            public List<DashboardStudentConsecutiveAbsenceRiskModel> StudentsWithConsecutiveAbsences { get; set; } = new();
            public List<DashboardStudentCombinedRiskModel> StudentsWithCombinedAcademicRisk { get; set; } = new();
            public List<DashboardCourseAttendanceRiskModel> CoursesAtRiskByAttendance { get; set; } = new();
            public List<DashboardCourseTrendRiskModel> CoursesWithAttendanceDecline { get; set; } = new();
            public List<DashboardCourseTrendRiskModel> CoursesWithPerformanceDecline { get; set; } = new();
            public List<DashboardCriticalCourseModel> CriticalCourses { get; set; } = new();
            public List<DashboardCriticalCourseModel> CoursesCurrentRisk { get; set; } = new();
            public List<DashboardCoursePendingFollowUpModel> CoursesPendingFollowUp { get; set; } = new();
            public List<DashboardOpenFollowUpModel> OpenFollowUps { get; set; } = new();
            public List<DashboardCourseTrendRiskModel> CourseTrendAlerts { get; set; } = new();
            public List<DashboardAcademicTrendModel> AcademicTrends { get; set; } = new();
            public List<DashboardAverageGradeByCourseModel> CoursesAtRiskByOverallAverage { get; set; } = new();
            public List<DashboardAverageGradeByCourseModel> CoursesAtRiskByManualAverage { get; set; } = new();

            public List<DashboardUpcomingAssignmentModel> UpcomingAssignments { get; set; } = new();
            public List<DashboardUpcomingClassModel> UpcomingClasses { get; set; } = new();
        }

        public class DashboardPeriodModel
        {
            public string Type { get; set; } = "academic-quarter";
            public string Strategy { get; set; } = "academic-quarter";
            public string Label { get; set; } = default!;
            public string MonthRangeLabel { get; set; } = default!;
            public DateOnly From { get; set; }
            public DateOnly To { get; set; }
            public int Year { get; set; }
            public int Month { get; set; }
            public int Quarter { get; set; }
        }

        public class DashboardTrendComparisonModel
        {
            public string Type { get; set; } = "previous-academic-quarter";
            public string Label { get; set; } = "trimestre anterior";
        }

        public class DashboardRollingWindowModel
        {
            public string Type { get; set; } = "rolling-days";
            public int Days { get; set; }
            public string Label { get; set; } = default!;
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
            public string? CursoDescripcion { get; set; }
            public List<string> ProfesoresNombres { get; set; } = new();
            public decimal AverageGrade { get; set; }
        }

        public class DashboardStudentAverageRiskModel
        {
            public int AlumnoId { get; set; }
            public string AlumnoNombre { get; set; } = default!;
            public string? AlumnoAvatarUrl { get; set; }
            public int CursoId { get; set; }
            public string CursoNombre { get; set; } = default!;
            public string? CursoDescripcion { get; set; }
            public decimal AverageGrade { get; set; }
            public int CalificacionesCount { get; set; }
        }

        public class DashboardStudentAttendanceRiskModel
        {
            public int AlumnoId { get; set; }
            public string AlumnoNombre { get; set; } = default!;
            public string? AlumnoAvatarUrl { get; set; }
            public int CursoId { get; set; }
            public string CursoNombre { get; set; } = default!;
            public string? CursoDescripcion { get; set; }
            public int Ausentes { get; set; }
            public int ClasesTotales { get; set; }
            public decimal AttendancePercentage { get; set; }
            public decimal? AverageGrade { get; set; }
        }

        public class DashboardCourseAttendanceRiskModel
        {
            public int CursoId { get; set; }
            public string CursoNombre { get; set; } = default!;
            public string? CursoDescripcion { get; set; }
            public List<string> ProfesoresNombres { get; set; } = new();
            public decimal AttendancePercentage { get; set; }
            public int Ausentes { get; set; }
            public int ExpectedAttendanceRecords { get; set; }
        }

        public class DashboardStudentConsecutiveAbsenceRiskModel
        {
            public int AlumnoId { get; set; }
            public string AlumnoNombre { get; set; } = default!;
            public string? AlumnoAvatarUrl { get; set; }
            public int CursoId { get; set; }
            public string CursoNombre { get; set; } = default!;
            public string? CursoDescripcion { get; set; }
            public int ConsecutiveAbsences { get; set; }
            public DateOnly LastAbsenceDate { get; set; }
            public decimal AttendancePercentage { get; set; }
            public decimal? AverageGrade { get; set; }
        }

        public class DashboardStudentCombinedRiskModel
        {
            public int AlumnoId { get; set; }
            public string AlumnoNombre { get; set; } = default!;
            public string? AlumnoAvatarUrl { get; set; }
            public int CursoId { get; set; }
            public string CursoNombre { get; set; } = default!;
            public string? CursoDescripcion { get; set; }
            public decimal AverageGrade { get; set; }
            public decimal AttendancePercentage { get; set; }
            public int Absences { get; set; }
        }

        public class DashboardCourseTrendRiskModel
        {
            public int CursoId { get; set; }
            public string CursoNombre { get; set; } = default!;
            public string? CursoDescripcion { get; set; }
            public List<string> ProfesoresNombres { get; set; } = new();
            public string Context { get; set; } = "trend";
            public string ContextLabel { get; set; } = "Caida respecto al trimestre anterior";
            public string PeriodLabel { get; set; } = default!;
            public string TrendType { get; set; } = default!;
            public decimal CurrentValue { get; set; }
            public decimal PreviousValue { get; set; }
            public decimal Delta { get; set; }
        }

        public class DashboardCriticalCourseModel
        {
            public int CursoId { get; set; }
            public string CursoNombre { get; set; } = default!;
            public string? CursoDescripcion { get; set; }
            public List<string> ProfesoresNombres { get; set; } = new();
            public decimal? AverageGrade { get; set; }
            public decimal? AttendancePercentage { get; set; }
            public int PendingCorrectionCount { get; set; }
            public int SignalsCount { get; set; }
            public string Context { get; set; } = "current-risk";
            public string ContextLabel { get; set; } = "Riesgo actual del trimestre";
            public string PeriodLabel { get; set; } = default!;
            public int StudentsAtRiskCurrentCount { get; set; }
            public int PendingFollowUpCount { get; set; }
            public CourseHealthModel Health { get; set; } = new();
        }

        public class DashboardCoursePendingFollowUpModel
        {
            public int CursoId { get; set; }
            public string CursoNombre { get; set; } = default!;
            public string? CursoDescripcion { get; set; }
            public List<string> ProfesoresNombres { get; set; } = new();
            public string Context { get; set; } = "pending-follow-up";
            public string ContextLabel { get; set; } = "Seguimiento pendiente";
            public string PeriodLabel { get; set; } = default!;
            public int QuarterNumber { get; set; }
            public int Year { get; set; }
            public string Level { get; set; } = CourseHealthLevels.FollowUp;
            public string Reason { get; set; } = default!;
            public decimal? AverageGrade { get; set; }
            public decimal? AttendancePercentage { get; set; }
            public string Description { get; set; } = default!;
        }

        public class DashboardOpenFollowUpModel
        {
            public string Id { get; set; } = default!;
            public string EntityType { get; set; } = default!;
            public int EntityId { get; set; }
            public int? AlumnoId { get; set; }
            public string? AlumnoNombre { get; set; }
            public string? AlumnoAvatarUrl { get; set; }
            public int? CursoId { get; set; }
            public string? CursoNombre { get; set; }
            public string? CursoDescripcion { get; set; }
            public string PeriodLabel { get; set; } = default!;
            public int QuarterNumber { get; set; }
            public int Year { get; set; }
            public string Reason { get; set; } = default!;
            public string Source { get; set; } = default!;
            public string Level { get; set; } = CourseHealthLevels.FollowUp;
            public decimal? AverageGrade { get; set; }
            public decimal? AttendancePercentage { get; set; }
            public List<DashboardOpenFollowUpGradeAlertModel> GradeAlerts { get; set; } = new();
            public string Href { get; set; } = default!;
        }

        public class DashboardOpenFollowUpGradeAlertModel
        {
            public int CalificacionId { get; set; }
            public string Titulo { get; set; } = default!;
            public TipoCalificacion Tipo { get; set; }
            public decimal Nota { get; set; }
            public DateOnly Fecha { get; set; }
        }

        public class DashboardAcademicTrendModel
        {
            public string Key { get; set; } = default!;
            public string Label { get; set; } = default!;
            public decimal? CurrentValue { get; set; }
            public decimal? PreviousValue { get; set; }
            public decimal? Delta { get; set; }
        }

        public class DashboardUpcomingAssignmentModel
        {
            public int TareaId { get; set; }
            public string Titulo { get; set; } = default!;
            public int CursoId { get; set; }
            public string CursoNombre { get; set; } = default!;
            public string? CursoDescripcion { get; set; }
            public DateTime FechaEntregaUtc { get; set; }
        }

        public class DashboardUpcomingClassModel
        {
            public int CursoId { get; set; }
            public string CursoNombre { get; set; } = default!;
            public string? CursoDescripcion { get; set; }
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
            public string? AlumnoAvatarUrl { get; set; }
            public int CursoId { get; set; }
            public string CursoNombre { get; set; } = default!;
            public string? CursoDescripcion { get; set; }
            public int CalificacionId { get; set; }
            public string Titulo { get; set; } = default!;
        public TipoCalificacion Tipo { get; set; }
        public decimal Nota { get; set; }
        public DateOnly Fecha { get; set; }
        public decimal? AverageGrade { get; set; }
    }
}
