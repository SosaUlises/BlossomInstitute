using BlossomInstitute.Domain.Entidades.Entrega;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.Entregas.Commands.CreateFeedbackEntrega
{
    public class CreateFeedbackEntregaModel
    {
        public string? Comentario { get; set; }
        public EstadoCorreccion Estado { get; set; } // Aprobado / Rehacer / Revisar
        public decimal? Nota { get; set; }   
        public string? ArchivoCorregidoUrl { get; set; }
        public string? ArchivoCorregidoNombre { get; set; }
    }
}
