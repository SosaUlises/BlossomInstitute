using BlossomInstitute.Application.DataBase.Curso.Shared;

namespace BlossomInstitute.Application.DataBase.Curso.Queries.GetAcademicProfile
{
    public class CourseAcademicProfileResponseModel
    {
        public CourseAcademicProfileCourseModel Course { get; set; } = new();
        public List<CourseAcademicProfileTeacherModel> Teachers { get; set; } = new();
        public CourseAcademicProfileStudentsModel Students { get; set; } = new();
        public CourseAcademicProfileMetricsModel AcademicMetrics { get; set; } = new();
        public CourseHealthModel Health { get; set; } = new();
        public List<CourseAcademicProfileAffectedStudentModel> StudentsRequiringFollowUp { get; set; } = new();
        public List<CourseAcademicProfileSignalModel> AcademicSignals { get; set; } = new();
        public List<CourseAcademicProfileActivityModel> RecentActivity { get; set; } = new();
    }

    public class CourseAcademicProfileCourseModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string Status { get; set; } = default!;
    }

    public class CourseAcademicProfileTeacherModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = default!;
        public string? AvatarUrl { get; set; }
    }

    public class CourseAcademicProfileStudentsModel
    {
        public int StudentsCount { get; set; }
    }

    public class CourseAcademicProfileMetricsModel
    {
        public decimal? AttendanceAverage { get; set; }
        public decimal? AcademicAverage { get; set; }
        public int StudentsAtRiskCount { get; set; }
        public int PendingCorrectionsCount { get; set; }
    }

    public class CourseAcademicProfileAffectedStudentModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = default!;
        public string? AvatarUrl { get; set; }
        public decimal? AttendancePercentage { get; set; }
        public decimal? AverageGrade { get; set; }
        public string Reason { get; set; } = default!;
    }

    public class CourseAcademicProfileSignalModel
    {
        public string Type { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Severity { get; set; } = "neutral";
    }

    public class CourseAcademicProfileActivityModel
    {
        public string Type { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Severity { get; set; } = "neutral";
        public DateTime? OccurredAtUtc { get; set; }
    }
}
