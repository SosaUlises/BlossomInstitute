namespace BlossomInstitute.Application.DataBase.Alumno.Queries.GetAcademicSummary
{
    public class AlumnoAcademicSummaryResponseModel
    {
        public AlumnoAcademicIdentityModel Student { get; set; } = new();
        public AlumnoAcademicPeriodModel Period { get; set; } = new();
        public AlumnoAcademicEnrollmentModel? CurrentCourse { get; set; }
        public List<AlumnoAcademicEnrollmentModel> CurrentEnrollments { get; set; } = new();
        public AlumnoAcademicAttendanceSummaryModel AttendanceSummary { get; set; } = new();
        public AlumnoAcademicGradesSummaryModel GradesSummary { get; set; } = new();
        public AlumnoAcademicHomeworkSummaryModel HomeworkSummary { get; set; } = new();
        public AlumnoAcademicStatusModel AcademicStatus { get; set; } = new();
        public List<AlumnoAcademicSignalModel> RecentSignals { get; set; } = new();
    }

    public class AlumnoAcademicIdentityModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string FullName { get; set; } = default!;
        public string? Email { get; set; }
        public long Dni { get; set; }
        public bool Active { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class AlumnoAcademicPeriodModel
    {
        public string Type { get; set; } = "academic-quarter";
        public string Label { get; set; } = default!;
        public string MonthRangeLabel { get; set; } = default!;
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public int Year { get; set; }
        public int Quarter { get; set; }
    }

    public class AlumnoAcademicEnrollmentModel
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = default!;
        public string? CourseDescription { get; set; }
        public string CourseStatus { get; set; } = default!;
        public string? TeacherName { get; set; }
        public string? TeacherAvatarUrl { get; set; }
        public bool IsMain { get; set; }
    }

    public class AlumnoAcademicAttendanceSummaryModel
    {
        public decimal? AttendancePercentage { get; set; }
        public int? PresentCount { get; set; }
        public int? AbsentCount { get; set; }
        public int? TotalClasses { get; set; }
        public int? ConsecutiveAbsences { get; set; }
        public bool? IsLowAttendance { get; set; }
    }

    public class AlumnoAcademicGradesSummaryModel
    {
        public decimal? AverageGrade { get; set; }
        public decimal? ManualAverageGrade { get; set; }
        public int? LowGradesCount { get; set; }
        public AlumnoAcademicGradeSignalModel? LatestLowGrade { get; set; }
        public AlumnoAcademicGradeSignalModel? LatestGrade { get; set; }
    }

    public class AlumnoAcademicGradeSignalModel
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Type { get; set; } = default!;
        public decimal Grade { get; set; }
        public DateOnly Date { get; set; }
    }

    public class AlumnoAcademicHomeworkSummaryModel
    {
        public int? PendingSubmissions { get; set; }
        public int? PendingCorrections { get; set; }
        public int? ApprovedCount { get; set; }
        public int? NeedsRevisionCount { get; set; }
    }

    public class AlumnoAcademicStatusModel
    {
        public string Level { get; set; } = "normal";
        public string Label { get; set; } = "Sin alertas academicas";
        public List<string> Reasons { get; set; } = new();
    }

    public class AlumnoAcademicSignalModel
    {
        public string Type { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Severity { get; set; } = "neutral";
        public DateOnly? Date { get; set; }
    }
}
