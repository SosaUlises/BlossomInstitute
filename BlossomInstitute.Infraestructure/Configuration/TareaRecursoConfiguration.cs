using BlossomInstitute.Domain.Entidades.Tarea;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlossomInstitute.Infraestructure.Configuration
{
    public class TareaRecursoConfiguration
    {
        public TareaRecursoConfiguration(EntityTypeBuilder<TareaRecursoEntity> b)
        {
            b.ToTable("TareaRecursos");
            b.HasKey(x => x.Id);

            b.Property(x => x.Url).IsRequired().HasMaxLength(2000);
            b.Property(x => x.Nombre).HasMaxLength(200);

            b.Property(x => x.Tipo)
                .HasConversion<int>()
                .IsRequired();

            b.HasIndex(x => new { x.TareaId, x.Tipo });
        }
    }
}

