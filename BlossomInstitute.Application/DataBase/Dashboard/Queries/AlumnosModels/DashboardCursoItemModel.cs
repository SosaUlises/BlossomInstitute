using BlossomInstitute.Domain.Entidades.Curso;

namespace BlossomInstitute.Application.DataBase.Dashboard.Queries.AlumnosModels
{
    public class DashboardCursoItemModel
    {
        public int CursoId { get; set; }
        public string CursoNombre { get; set; } = default!;
        public int Anio { get; set; }
        public string? Descripcion { get; set; }
        public EstadoCurso Estado { get; set; }
    }
}
