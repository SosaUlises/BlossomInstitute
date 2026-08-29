namespace BlossomInstitute.Application.DataBase.Dashboard.Queries.AlumnosModels
{
    public class DashboardTareaPendienteItemModel
    {
        public int TareaId { get; set; }
        public int CursoId { get; set; }
        public string CursoNombre { get; set; } = default!;
        public string Titulo { get; set; } = default!;
        public DateTime? FechaEntregaUtc { get; set; }
        public bool Vencida { get; set; }
    }
}
