using BlossomInstitute.Domain.Entidades.Curso;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlossomInstitute.Infraestructure.Configuration
{
    public class CursoConfiguration
    {
        public CursoConfiguration(EntityTypeBuilder<CursoEntity> b)
        {
            b.ToTable("Cursos");
            b.HasKey(x => x.Id);

            b.Property(x => x.Nombre).IsRequired().HasMaxLength(100);
            b.Property(x => x.Anio).IsRequired();
            b.Property(x => x.Descripcion).HasMaxLength(1000);
            b.Property(x => x.ThemeIcon).HasMaxLength(200);
            b.Property(x => x.Estado).IsRequired();

            b.HasIndex(x => new { x.Anio, x.Nombre }).IsUnique(false);
        }
    }
}
