using BlossomInstitute.Domain.Entidades.Calificacion;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Commands.Update
{
    public class UpdatePlantillaCalificacionModel
    {
        public TipoCalificacion Tipo { get; set; }
        public string Titulo { get; set; } = default!;
        public string? Descripcion { get; set; }
        public List<UpdatePlantillaCalificacionDetalleModel> Detalles { get; set; } = new();
    }
}


