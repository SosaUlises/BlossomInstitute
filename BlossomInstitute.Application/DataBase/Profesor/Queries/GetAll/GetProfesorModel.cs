namespace BlossomInstitute.Application.DataBase.Profesor.Queries.GetAll
{
    public class GetProfesorCourseModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
    }

    public class GetProfesorModel
    {
        public int Id { get; set; }
        public string Email { get; set; } = default!;
        public string Nombre { get; set; } = default!;
        public string Apellido { get; set; } = default!;
        public long Dni { get; set; }
        public string? Telefono { get; set; }
        public string? AvatarUrl { get; set; }
        public bool Activo { get; set; }
        public int AssignedCoursesCount { get; set; }
        public List<GetProfesorCourseModel> AssignedCourses { get; set; } = new();
        public int StudentsCount { get; set; }
        public int PendingCorrectionsCount { get; set; }
        public int ClassesThisWeek { get; set; }
        public int UnloadedAttendanceCount { get; set; }
        public int CoursesAtRiskCount { get; set; }
        public bool RequiresFollowUp { get; set; }
        public string MainSignal { get; set; } = "Sin señales pendientes";
    }
}
