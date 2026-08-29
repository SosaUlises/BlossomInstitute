using Microsoft.AspNetCore.Http;

namespace BlossomInstitute.Application.External.Archivos
{
    public class SubirArchivoRequest
    {
        public IFormFile File { get; set; } = default!;
        public string? Folder { get; set; }
    }
    public class EliminarArchivoRequest
    {
        public string StorageKey { get; set; } = default!;
    }
}
