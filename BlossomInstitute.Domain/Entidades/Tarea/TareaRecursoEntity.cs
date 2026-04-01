using BlossomInstitute.Domain.Entidades.Common;

namespace BlossomInstitute.Domain.Entidades.Tarea
{
    public class TareaRecursoEntity
    {
        public int Id { get; set; }

        public int TareaId { get; set; }
        public TareaEntity Tarea { get; set; } = default!;

        public TipoRecursoTarea Tipo { get; set; }

        public string Url { get; set; } = default!;
        public string? Nombre { get; set; }

        public StorageProviderType? StorageProvider { get; set; }
        public string? StorageKey { get; set; }
        public string? ContentType { get; set; }
        public long? SizeBytes { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}


