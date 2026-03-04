namespace BlossomInstitute.Application.DataBase.Entregas.Queries.Models
{
    public class FeedbackHistoryItemModel
    {
        public int FeedbackId { get; set; }
        public bool EsVigente { get; set; }
        public int Estado { get; set; }
        public decimal? Nota { get; set; }
        public string? Comentario { get; set; }
        public DateTime FechaCorreccionUtc { get; set; }

        public string? ArchivoCorregidoUrl { get; set; }
        public string? ArchivoCorregidoNombre { get; set; }
    }
}
