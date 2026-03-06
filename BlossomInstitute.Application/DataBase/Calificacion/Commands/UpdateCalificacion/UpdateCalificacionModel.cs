using BlossomInstitute.Domain.Entidades.Calificacion;

namespace BlossomInstitute.Application.DataBase.Calificacion.Commands.UpdateCalificacion
{
    public class UpdateCalificacionModel
    {
        public TipoCalificacion Tipo { get; set; }
        public string Titulo { get; set; } = default!;
        public string? Descripcion { get; set; }
        public decimal Nota { get; set; }
        public DateOnly Fecha { get; set; }

        public int? TareaId { get; set; }
        public int? EntregaId { get; set; }
    }
}
