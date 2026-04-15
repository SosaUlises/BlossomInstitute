using BlossomInstitute.Domain.Entidades.Calificacion;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Command.CreatePlantilla
{
    public class CreatePlantillaCalificacionModel
    {
        public TipoCalificacion Tipo { get; set; }
        public string Titulo { get; set; } = default!;
        public string? Descripcion { get; set; }
        public List<CreatePlantillaCalificacionDetalleModel> Detalles { get; set; } = new();
    }
}

