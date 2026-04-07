using BlossomInstitute.Domain.Entidades.Entrega;

namespace BlossomInstitute.Application.DataBase.Entregas.Queries.Models
{
    public class FeedbackHistoryItemModel
    {
        public int FeedbackId { get; set; }
        public bool EsVigente { get; set; }
        public EstadoCorreccion Estado { get; set; }
        public decimal? Nota { get; set; }
        public string? Comentario { get; set; }
        public DateTime FechaCorreccionUtc { get; set; }

        public List<FeedbackAdjuntoItemModel> Adjuntos { get; set; } = new();
    }
}
