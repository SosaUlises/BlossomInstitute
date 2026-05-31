namespace BlossomInstitute.Application.DataBase.Profesor.Queries.GetAcademicSummary
{
    public class ProfesorAcademicSummaryResponseModel
    {
        public ProfesorAcademicIdentityModel Teacher { get; set; } = new();
        public int AssignedCoursesCount { get; set; }
        public List<ProfesorAcademicCourseModel> AssignedCourses { get; set; } = new();
        public int StudentsCount { get; set; }
        public int PendingCorrectionsCount { get; set; }
        public int UnloadedAttendanceCount { get; set; }
        public int ClassesThisWeek { get; set; }
        public ProfesorOperationalStatusModel OperationalStatus { get; set; } = new();
        public List<ProfesorRecentActivityModel> RecentActivity { get; set; } = new();
    }

    public class ProfesorAcademicIdentityModel
    {
        public int Id { get; set; }
        public string? AvatarUrl { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string? Email { get; set; }
        public bool Active { get; set; }
    }

    public class ProfesorAcademicCourseModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public int StudentsCount { get; set; }
        public decimal? AttendanceAverage { get; set; }
        public decimal? AverageGrade { get; set; }
        public bool RequiresAttention { get; set; }
    }

    public class ProfesorOperationalStatusModel
    {
        public string Level { get; set; } = "normal";
        public string Label { get; set; } = "Normal";
        public List<string> Reasons { get; set; } = new();
    }

    public class ProfesorRecentActivityModel
    {
        public string Type { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Severity { get; set; } = "neutral";
        public int? CourseId { get; set; }
        public string? CourseName { get; set; }
        public DateTime? OccurredAtUtc { get; set; }
    }
}
