using BlossomInstitute.Domain.Entidades.Common;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace BlossomInstitute.Application.DataBase.CloudinaryService.Commands.UploadFile
{
    public class CloudinaryFileStorageService : IFileStorageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly CloudinaryStorageOptions _options;

        public CloudinaryFileStorageService(IOptions<CloudinaryStorageOptions> options)
        {
            _options = options.Value;

            var account = new Account(
                _options.CloudName,
                _options.ApiKey,
                _options.ApiSecret
            );

            _cloudinary = new Cloudinary(account);


            _cloudinary.Api.Secure = true;
        }

        public async Task<UploadFileResponseModel> UploadAsync(
            IFormFile file,
            string folder,
            CancellationToken ct = default)
        {
            if (file == null || file.Length == 0)
                throw new InvalidOperationException("Archivo inválido");

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

            return new UploadFileResponseModel
            {
                Url = result.SecureUrl?.ToString() ?? result.Url?.ToString() ?? "",
                Nombre = file.FileName,
                StorageProvider = StorageProviderType.Cloudinary,
                StorageKey = result.PublicId,
                ContentType = file.ContentType,
                SizeBytes = file.Length
            };
        }

        public async Task DeleteAsync(string storageKey, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(storageKey))
                throw new InvalidOperationException("StorageKey inválido");

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
    }
}


