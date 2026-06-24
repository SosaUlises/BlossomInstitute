using BlossomInstitute.Domain.Entidades.Curso;

namespace BlossomInstitute.Application.DataBase.Curso.Queries.GetCursoById
{
    public class GetCursoByIdModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = default!;
        public int Anio { get; set; }
        public string? Descripcion { get; set; }
        public string? ThemeIcon { get; set; }
        public EstadoCurso Estado { get; set; }

        public List<GetCursoHorarioModel> Horarios { get; set; } = new();

        public int CantidadProfesores { get; set; }
        public int CantidadAlumnos { get; set; }
    }
}
