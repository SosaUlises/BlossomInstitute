using BlossomInstitute.Domain.Entidades.Entrega;

namespace BlossomInstitute.Application.DataBase.Entregas.Queries.Models
{
    public class EntregaAdjuntoModel
    {
        public int Id { get; set; }
        public string Url { get; set; } = "";
        public string Nombre { get; set; } = "";
        public TipoAdjunto Tipo { get; set; }
    }
}
