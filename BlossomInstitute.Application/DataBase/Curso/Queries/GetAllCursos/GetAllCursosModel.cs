using BlossomInstitute.Domain.Entidades.Curso;

using BlossomInstitute.Application.DataBase.Curso.Shared;

namespace BlossomInstitute.Application.DataBase.Curso.Queries.GetAllCursos
{
    public class GetAllCursosModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = default!;
        public int Anio { get; set; }
        public EstadoCurso Estado { get; set; }

        public int CantidadHorarios { get; set; }
        public int CantidadProfesores { get; set; }
        public int CantidadAlumnos { get; set; }

        public List<string> AvatarUrls { get; set; } = new();
        public List<GetAllCursosTeacherModel> Teachers { get; set; } = new();
        public List<string> TeacherNames { get; set; } = new();
        public int StudentsCount { get; set; }
        public decimal? AttendanceAverage { get; set; }
        public decimal? AcademicAverage { get; set; }
        public int PendingCorrectionsCount { get; set; }
        public int StudentsAtRiskCount { get; set; }
        public bool RequiresAttention { get; set; }
        public CourseHealthModel HealthStatus { get; set; } = new();
        public string MainSignal { get; set; } = default!;
    }

    public class GetAllCursosTeacherModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string? AvatarUrl { get; set; }
    }

}
