namespace BlossomInstitute.Application.DataBase.Alumno.Queries.GetAll
{
    public class GetAlumnoModel
    {
        public int Id { get; set; }
        public string Email { get; set; } = default!;
        public string Nombre { get; set; } = default!;
        public string Apellido { get; set; } = default!;
        public long Dni { get; set; }
        public string? Telefono { get; set; }
        public bool Activo { get; set; }
        public bool IsActive { get; set; }
        public string? AvatarUrl { get; set; }
        public int? CurrentCourseId { get; set; }
        public string? CurrentCourseName { get; set; }
        public string? CurrentCourseDescription { get; set; }
        public bool HasActiveEnrollment { get; set; }
        public bool IsWithoutCourse { get; set; }
        public decimal? AttendancePercentage { get; set; }
        public decimal? AverageGrade { get; set; }
        public GetAlumnoLatestLowGradeModel? LatestLowGrade { get; set; }
        public int? ConsecutiveAbsences { get; set; }
        public string AcademicStatusLevel { get; set; } = "normal";
        public string AcademicStatusLabel { get; set; } = "Sin alertas academicas";
        public List<string> AcademicReasons { get; set; } = new();
    }

    public class GetAlumnoLatestLowGradeModel
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; } = default!;
        public string Title { get; set; } = default!;
        public decimal Grade { get; set; }
        public DateOnly Date { get; set; }
    }
}
