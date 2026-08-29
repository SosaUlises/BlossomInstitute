using BlossomInstitute.Domain.Entidades.Calificaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlossomInstitute.Infraestructure.Configuration
{
    public class CalificacionConfiguration
    {
        public CalificacionConfiguration(EntityTypeBuilder<CalificacionEntity> entity)
        {
            entity.ToTable("Calificaciones");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Tipo)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(x => x.Titulo)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Descripcion)
                .HasMaxLength(500);

            entity.Property(x => x.Nota)
                .HasPrecision(5, 2) 
                .IsRequired();

            entity.Property(x => x.Fecha)
                .HasConversion(
                    v => v.ToDateTime(TimeOnly.MinValue),
                    v => DateOnly.FromDateTime(v))
                .HasColumnType("date")
                .IsRequired();

            entity.Property(x => x.TieneDetalleSkills)
                .IsRequired();

            entity.Property(x => x.Archivado)
                .IsRequired();

            entity.Property(x => x.ArchivadoPorTarea)
                .IsRequired();

            entity.Property(x => x.CreatedAtUtc)
                .IsRequired();

            entity.Property(x => x.UpdatedAtUtc);

            entity.HasOne(x => x.Curso)
                .WithMany()
                .HasForeignKey(x => x.CursoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Alumno)
                .WithMany()
                .HasForeignKey(x => x.AlumnoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Tarea)
                .WithMany()
                .HasForeignKey(x => x.TareaId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.Entrega)
                .WithMany()
                .HasForeignKey(x => x.EntregaId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(x => x.Detalles)
                .WithOne(x => x.Calificacion)
                .HasForeignKey(x => x.CalificacionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.CursoId, x.AlumnoId, x.Archivado });

            entity.HasIndex(x => new { x.CursoId, x.AlumnoId, x.Fecha });

            entity.HasIndex(x => new { x.CursoId, x.AlumnoId, x.Tipo, x.Archivado });

            entity.HasIndex(x => new { x.CursoId, x.AlumnoId, x.TareaId, x.EntregaId, x.Archivado })
                .IsUnique();
        }
    }
}
