using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Domain.Entidades.Entrega
{
    public class EntregaAdjuntoEntity
    {
        public int Id { get; set; }

        public int EntregaId { get; set; }
        public EntregaEntity Entrega { get; set; } = default!;

        public TipoAdjunto Tipo { get; set; }

        // Si es Link: URL. Si es Archivo: URL/path al storage.
        public string Url { get; set; } = default!;
        public string? Nombre { get; set; }
    }
}
