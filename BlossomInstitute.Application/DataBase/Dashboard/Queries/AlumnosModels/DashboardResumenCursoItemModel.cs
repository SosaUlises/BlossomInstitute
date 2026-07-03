namespace BlossomInstitute.Application.DataBase.Dashboard.Queries.AlumnosModels
{
    public class DashboardResumenCursoItemModel
    {
        public int CursoId { get; set; }
        public string CursoNombre { get; set; } = default!;
        public decimal? Promedio { get; set; }
        public decimal PorcentajeAsistencia { get; set; }
        public int TareasPendientes { get; set; }
    }
}
