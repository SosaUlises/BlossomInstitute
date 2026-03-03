using BlossomInstitute.Domain.Entidades.Entrega;

namespace BlossomInstitute.Application.DataBase.Entregas.Commands.UpsertEntregaAlumno
{
    public class UpsertEntregaAdjuntoModel
    {
        public TipoAdjunto Tipo { get; set; }
        public string Url { get; set; } = default!;
        public string? Nombre { get; set; }
    }
}
