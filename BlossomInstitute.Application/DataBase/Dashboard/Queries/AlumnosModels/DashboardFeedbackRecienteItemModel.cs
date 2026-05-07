namespace BlossomInstitute.Application.DataBase.Dashboard.Queries.Models
{
    public class DashboardFeedbackRecienteItemModel
    {
        public int CursoId { get; set; }
        public string CursoNombre { get; set; } = default!;
        public int TareaId { get; set; }
        public string TituloTarea { get; set; } = default!;
        public int EntregaId { get; set; }
        public int FeedbackId { get; set; }
        public int Estado { get; set; }
        public string? Comentario { get; set; }
        public decimal? Nota { get; set; }
        public DateTime FechaCorreccionUtc { get; set; }
    }
}
