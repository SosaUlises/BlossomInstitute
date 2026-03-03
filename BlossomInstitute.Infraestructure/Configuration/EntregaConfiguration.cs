using BlossomInstitute.Domain.Entidades.Entrega;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Infraestructure.Configuration
{
    public class EntregaConfiguration
    {
        public EntregaConfiguration(EntityTypeBuilder<EntregaEntity> b)
        {
            b.ToTable("Entregas");
            b.HasKey(x => x.Id);

            b.Property(x => x.Texto).HasMaxLength(8000);

            b.Property(x => x.FechaEntregaUtc).IsRequired();

            b.Property(x => x.Estado)
                .HasConversion<int>()
                .IsRequired();

            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc);

            b.HasOne(x => x.Tarea)
                .WithMany() 
                .HasForeignKey(x => x.TareaId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Alumno)
                .WithMany()
                .HasForeignKey(x => x.AlumnoId)
                .OnDelete(DeleteBehavior.Restrict);

            // UNA SOLA ENTREGA POR (Tarea, Alumno)
            b.HasIndex(x => new { x.TareaId, x.AlumnoId })
                .IsUnique();

            b.HasMany(x => x.Adjuntos)
                .WithOne(a => a.Entrega)
                .HasForeignKey(a => a.EntregaId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.Feedbacks)
                 .WithOne(f => f.Entrega)
                 .HasForeignKey(f => f.EntregaId)
                 .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.TareaId, x.Estado });
        }
    }
}
