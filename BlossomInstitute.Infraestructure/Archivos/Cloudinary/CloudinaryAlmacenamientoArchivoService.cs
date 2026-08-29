using BlossomInstitute.Application.External.Archivos;
using BlossomInstitute.Domain.Entidades.Common;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BlossomInstitute.Infraestructure.Archivos.Cloudinary
{
    public class CloudinaryAlmacenamientoArchivoService : IAlmacenamientoArchivoService
    {
        private readonly CloudinaryDotNet.Cloudinary _cloudinary;
        private readonly OpcionesCloudinary _options;

        public CloudinaryAlmacenamientoArchivoService(IOptions<OpcionesCloudinary> options)
        {
            _options = options.Value;

            var account = new Account(
                _options.CloudName,
                _options.ApiKey,
                _options.ApiSecret);

            _cloudinary = new CloudinaryDotNet.Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }

        public async Task<ArchivoSubidoResponseModel> SubirAsync(
            IFormFile file,
            string folder,
            CancellationToken ct = default)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("Archivo invalido");

            await using var stream = file.OpenReadStream();

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = string.IsNullOrWhiteSpace(folder)
                    ? _options.Folder
                    : $"{_options.Folder}/{folder.Trim('/')}",
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new InvalidOperationException(result.Error.Message);

            return new ArchivoSubidoResponseModel
            {
                Url = result.SecureUrl?.ToString() ?? result.Url?.ToString() ?? "",
                Nombre = file.FileName,
                StorageProvider = StorageProviderType.Cloudinary,
                StorageKey = result.PublicId,
                ContentType = file.ContentType,
                SizeBytes = file.Length
            };
        }

        public async Task<ArchivoSubidoResponseModel> SubirAvatarAsync(
            IFormFile file,
            CancellationToken ct = default)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("Archivo invalido");

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = $"{_options.Folder}/avatars",
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false,
                Transformation = new Transformation()
                    .Width(512)
                    .Height(512)
                    .Crop("fill")
                    .Gravity("auto")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new InvalidOperationException(result.Error.Message);

            return new ArchivoSubidoResponseModel
            {
                Url = result.SecureUrl?.ToString() ?? result.Url?.ToString() ?? "",
                Nombre = file.FileName,
                StorageProvider = StorageProviderType.Cloudinary,
                StorageKey = result.PublicId,
                ContentType = file.ContentType,
                SizeBytes = file.Length
            };
        }

        public async Task EliminarAsync(string storageKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(storageKey))
                throw new InvalidOperationException("StorageKey invalido");

            var deleteParams = new DeletionParams(storageKey)
            {
                ResourceType = ResourceType.Raw
            };

            var result = await _cloudinary.DestroyAsync(deleteParams);

            if (result.Error != null)
                throw new InvalidOperationException(result.Error.Message);

            if (result.Result != "ok" && result.Result != "not found")
                throw new InvalidOperationException($"No se pudo eliminar el archivo. Resultado: {result.Result}");
        }

        public async Task EliminarImagenAsync(string storageKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(storageKey))
                throw new InvalidOperationException("StorageKey invalido");

            var deleteParams = new DeletionParams(storageKey)
            {
                ResourceType = ResourceType.Image
            };

            var result = await _cloudinary.DestroyAsync(deleteParams);

            if (result.Error != null)
                throw new InvalidOperationException(result.Error.Message);

            if (result.Result != "ok" && result.Result != "not found")
                throw new InvalidOperationException($"No se pudo eliminar el archivo. Resultado: {result.Result}");
        }
    }
}
