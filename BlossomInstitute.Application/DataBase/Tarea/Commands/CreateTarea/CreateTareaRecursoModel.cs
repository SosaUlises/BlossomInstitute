using BlossomInstitute.Domain.Entidades.Common;
using BlossomInstitute.Domain.Entidades.Tarea;

namespace BlossomInstitute.Application.DataBase.Tarea.Commands.CreateTarea
{
    public class CreateTareaRecursoModel
    {
        public TipoRecursoTarea Tipo { get; set; }
        public string? Url { get; set; }
        public string? Nombre { get; set; }

        public StorageProviderType? StorageProvider { get; set; }
        public string? StorageKey { get; set; }
        public string? ContentType { get; set; }
        public long? SizeBytes { get; set; }
    }
}

