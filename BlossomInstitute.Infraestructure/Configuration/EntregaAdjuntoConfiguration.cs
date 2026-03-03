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
    public class EntregaAdjuntoConfiguration
    {
        public EntregaAdjuntoConfiguration(EntityTypeBuilder<EntregaAdjuntoEntity> b)
        {
            b.ToTable("EntregaAdjuntos");
            b.HasKey(x => x.Id);

            b.Property(x => x.Tipo)
                .HasConversion<int>()
                .IsRequired();

            b.Property(x => x.Url)
                .IsRequired()
                .HasMaxLength(2000);

            b.Property(x => x.Nombre)
                .HasMaxLength(200);

            b.HasIndex(x => new { x.EntregaId, x.Tipo });
        }
    }
}
