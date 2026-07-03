using BlossomInstitute.Domain.Entidades.Entrega;

namespace BlossomInstitute.Application.DataBase.Dashboard.Queries.AlumnosModels
{
    public class DashboardUltimaEntregaItemModel
    {
        public int EntregaId { get; set; }
        public int TareaId { get; set; }
        public int CursoId { get; set; }
        public string CursoNombre { get; set; } = default!;
        public string TituloTarea { get; set; } = default!;
        public DateTime FechaEntregaUtc { get; set; }
        public EstadoEntrega EstadoEntrega { get; set; }
    }
}
