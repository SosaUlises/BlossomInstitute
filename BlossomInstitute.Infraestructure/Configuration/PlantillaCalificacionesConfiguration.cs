using BlossomInstitute.Domain.Entidades.Calificacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlossomInstitute.Infraestructure.Configuration
{
    public class PlantillaCalificacionesConfiguration
    {
        public PlantillaCalificacionesConfiguration(EntityTypeBuilder<PlantillaCalificacionEntity> entity)
        {

                entity.ToTable("PlantillasCalificaciones");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Titulo)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.Descripcion)
                    .HasMaxLength(500);

                entity.HasOne(x => x.Curso)
                    .WithMany()
                    .HasForeignKey(x => x.CursoId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Profesor)
                    .WithMany()
                    .HasForeignKey(x => x.ProfesorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(x => x.Detalles)
                    .WithOne(x => x.PlantillaCalificacion)
                    .HasForeignKey(x => x.PlantillaCalificacionId)
                    .OnDelete(DeleteBehavior.Cascade);
        }
    }

}
