using BlossomInstitute.Domain.Entidades.Calificacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlossomInstitute.Infraestructure.Configuration
{
    internal class PlantillaCalificacionesDetalleConfiguration
    {
        public PlantillaCalificacionesDetalleConfiguration(EntityTypeBuilder<PlantillaCalificacionDetalleEntity> entity)
        {

            entity.ToTable("PlantillasCalificacionesDetalles");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.PuntajeMaximo)
                .HasColumnType("decimal(18,2)");

        }
    }
}
