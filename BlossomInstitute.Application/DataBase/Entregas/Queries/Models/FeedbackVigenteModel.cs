namespace BlossomInstitute.Application.DataBase.Entregas.Queries.Models
{
    public class FeedbackVigenteModel
    {
        public int FeedbackId { get; set; }
        public int Estado { get; set; } // enum EstadoCorreccion
        public decimal? Nota { get; set; }
        public DateTime FechaCorreccionUtc { get; set; }
    }
}
