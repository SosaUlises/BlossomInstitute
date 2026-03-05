using BlossomInstitute.Application.DataBase.Entregas.Queries.Models;

namespace BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteEntregaByTarea
{
    public class EntregaReporteListItemModel
    {
        public int AlumnoId { get; set; }
        public string AlumnoNombre { get; set; } = "";
        public string AlumnoApellido { get; set; } = "";
        public long AlumnoDni { get; set; }

        public EstadoEntregaReporte Estado { get; set; }

        public int? EntregaId { get; set; }
        public DateTime? FechaEntregaUtc { get; set; }

        public bool TieneAdjuntos { get; set; }

        public FeedbackVigenteModel? FeedbackVigente { get; set; }
    }
}
