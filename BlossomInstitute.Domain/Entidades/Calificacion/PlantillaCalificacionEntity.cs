using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Entidades.Profesor;

namespace BlossomInstitute.Domain.Entidades.Calificacion
{
    public class PlantillaCalificacionEntity
    {
        public int Id { get; set; }

        public int CursoId { get; set; }
        public CursoEntity Curso { get; set; } = default!;

        public int ProfesorId { get; set; }
        public ProfesorEntity Profesor { get; set; } = default!;

        public TipoCalificacion Tipo { get; set; } // Solo Quiz o Test
        public string Titulo { get; set; } = default!;
        public string? Descripcion { get; set; }

        public bool Activa { get; set; } = true;
        public bool Archivada { get; set; } = false;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        public List<PlantillaCalificacionDetalleEntity> Detalles { get; set; } = new();
    }
}
