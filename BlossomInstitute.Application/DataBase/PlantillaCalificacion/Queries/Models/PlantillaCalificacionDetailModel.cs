using BlossomInstitute.Domain.Entidades.Calificacion;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Queries.Models
{

    public class PlantillaCalificacionDetailModel
    {
        public int Id { get; set; }
        public int CursoId { get; set; }
        public TipoCalificacion Tipo { get; set; }
        public string Titulo { get; set; } = default!;
        public string? Descripcion { get; set; }
        public bool TieneDetalleSkills { get; set; }
        public decimal? PuntajeMaximoTotal { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public List<PlantillaCalificacionDetalleModel> Detalles { get; set; } = new();
    }
}

