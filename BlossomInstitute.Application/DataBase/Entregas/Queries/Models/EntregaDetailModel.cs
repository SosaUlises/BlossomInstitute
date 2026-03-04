namespace BlossomInstitute.Application.DataBase.Entregas.Queries.Models
{
    public class EntregaDetailModel
    {
        public int EntregaId { get; set; }
        public int TareaId { get; set; }
        public int AlumnoId { get; set; }

        public string? Texto { get; set; }
        public DateTime FechaEntregaUtc { get; set; }
        public int EstadoEntrega { get; set; }

        public List<EntregaAdjuntoModel> Adjuntos { get; set; } = new();
        public FeedbackVigenteModel? FeedbackVigente { get; set; }
        public List<FeedbackHistoryItemModel> FeedbackHistorial { get; set; } = new();
    }
}
