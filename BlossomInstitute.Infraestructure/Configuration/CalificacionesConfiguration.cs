using BlossomInstitute.Domain.Entidades.Calificaciones;
using BlossomInstitute.Domain.Entidades.Clase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlossomInstitute.Infraestructure.Configuration
{
    public class CalificacionConfiguration 
    {
        public CalificacionConfiguration(EntityTypeBuilder<CalificacionEntity> b)
        {

            b.ToTable("Calificaciones");

            b.HasKey(x => x.Id);

            b.Property(x => x.NombreEvaluacion).HasMaxLength(100).IsRequired();
            b.Property(x => x.Nota).HasPrecision(4, 2).IsRequired();
            b.Property(x => x.Fecha).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();

            b.HasIndex(x => new { x.CursoId, x.AlumnoId });

            b.HasOne(x => x.Curso)
                .WithMany()
                .HasForeignKey(x => x.CursoId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Alumno)
                .WithMany()
                .HasForeignKey(x => x.AlumnoId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
