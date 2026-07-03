using BlossomInstitute.Domain.Entidades.Calificacion;

namespace BlossomInstitute.Application.DataBase.Dashboard.Queries.AlumnosModels
{
    public class DashboardUltimaCalificacionItemModel
    {
        public int CalificacionId { get; set; }
        public int CursoId { get; set; }
        public string CursoNombre { get; set; } = default!;
        public TipoCalificacion Tipo { get; set; }
        public string Titulo { get; set; } = default!;
        public decimal Nota { get; set; }
        public DateOnly Fecha { get; set; }
    }
}
