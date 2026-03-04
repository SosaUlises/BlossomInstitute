namespace BlossomInstitute.Application.DataBase.Entregas.Queries.Models
{
    public class EntregaListItemModel
    {
        public int EntregaId { get; set; }
        public int AlumnoId { get; set; }
        public string AlumnoNombre { get; set; } = "";
        public string AlumnoApellido { get; set; } = "";
        public long AlumnoDni { get; set; }

        public DateTime FechaEntregaUtc { get; set; }
        public int EstadoEntrega { get; set; }
        public bool TieneAdjuntos { get; set; }

        public FeedbackVigenteModel? FeedbackVigente { get; set; }
    }
}
