using BlossomInstitute.Domain.Entidades.Tarea;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlossomInstitute.Infraestructure.Configuration
{
    public class TareaConfiguration
    {
        public TareaConfiguration(EntityTypeBuilder<TareaEntity> b)
        {
            b.ToTable("Tareas");
            b.HasKey(x => x.Id);

            b.Property(x => x.Titulo).IsRequired().HasMaxLength(120);
            b.Property(x => x.Consigna).HasMaxLength(4000);

            b.Property(x => x.Estado)
                .HasConversion<int>()
                .IsRequired();

            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc);

            b.HasOne(x => x.Curso)
                .WithMany() 
                .HasForeignKey(x => x.CursoId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Profesor)
                .WithMany()
                .HasForeignKey(x => x.ProfesorId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasMany(x => x.Recursos)
                .WithOne(r => r.Tarea)
                .HasForeignKey(r => r.TareaId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.CursoId, x.Estado });
        }
    }
}
