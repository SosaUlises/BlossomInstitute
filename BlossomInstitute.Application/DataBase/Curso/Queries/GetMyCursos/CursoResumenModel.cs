using BlossomInstitute.Domain.Entidades.Curso;

namespace BlossomInstitute.Application.DataBase.Curso.Queries.GetMyCursos
{
    public class CursoResumenModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = default!;
        public int Anio { get; set; }
        public string? ThemeIcon { get; set; }
        public EstadoCurso Estado { get; set; }
        public int CantidadHorarios { get; set; }
    }
}
