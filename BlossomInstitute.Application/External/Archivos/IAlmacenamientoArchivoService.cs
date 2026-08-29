using Microsoft.AspNetCore.Http;

namespace BlossomInstitute.Application.External.Archivos
{
    public interface IAlmacenamientoArchivoService
    {
        Task<ArchivoSubidoResponseModel> SubirAsync(
            IFormFile file,
            string folder,
            CancellationToken ct = default);

        Task<ArchivoSubidoResponseModel> SubirAvatarAsync(
            IFormFile file,
            CancellationToken ct = default);

        Task EliminarAsync(string storageKey, CancellationToken ct = default);

        Task EliminarImagenAsync(string storageKey, CancellationToken ct = default);
    }
}
