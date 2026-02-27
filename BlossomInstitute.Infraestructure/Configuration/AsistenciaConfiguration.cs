using BlossomInstitute.Domain.Entidades.Clase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlossomInstitute.Infraestructure.Configuration
{
    public class AsistenciaConfiguration
    {
        public AsistenciaConfiguration(EntityTypeBuilder<AsistenciaEntity> entity)
        {
            entity.ToTable("Asistencias");

            entity.HasKey(x => x.Id);

            entity.HasIndex(x => new { x.ClaseId, x.AlumnoId })
                  .IsUnique();

            entity.Property(x => x.Estado)
                  .HasConversion<int>()
                  .IsRequired();

            entity.HasOne(x => x.Clase)
                  .WithMany(c => c.Asistencias)
                  .HasForeignKey(x => x.ClaseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Alumno)
                  .WithMany() 
                  .HasForeignKey(x => x.AlumnoId)
                  .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
