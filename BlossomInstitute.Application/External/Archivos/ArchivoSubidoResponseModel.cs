using BlossomInstitute.Domain.Entidades.Common;

namespace BlossomInstitute.Application.External.Archivos
{
    public class ArchivoSubidoResponseModel
    {
        public string Url { get; set; } = default!;
        public string Nombre { get; set; } = default!;
        public StorageProviderType StorageProvider { get; set; }
        public string StorageKey { get; set; } = default!;
        public string? ContentType { get; set; }
        public long SizeBytes { get; set; }
    }
}
