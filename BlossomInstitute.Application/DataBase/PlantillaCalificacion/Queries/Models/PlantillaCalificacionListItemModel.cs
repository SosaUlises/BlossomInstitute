using BlossomInstitute.Domain.Entidades.Calificacion;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Queries.Models
{
    public class PlantillaCalificacionListItemModel
    {
        public int Id { get; set; }
        public int CursoId { get; set; }
        public TipoCalificacion Tipo { get; set; }
        public string Titulo { get; set; } = default!;
        public string? Descripcion { get; set; }
        public bool TieneDetalleSkills { get; set; }
        public int CantidadSkills { get; set; }
        public decimal? PuntajeMaximoTotal { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}

