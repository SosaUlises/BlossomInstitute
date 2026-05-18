using Microsoft.AspNetCore.Http;

namespace BlossomInstitute.Application.DataBase.CloudinaryService.Commands.UploadFile
{
    public interface IFileStorageService
    {
        Task<UploadFileResponseModel> UploadAsync(
            IFormFile file,
            string folder,
            CancellationToken ct = default);

        Task<UploadFileResponseModel> UploadAvatarAsync(
            IFormFile file,
            CancellationToken ct = default);

        Task DeleteAsync(string storageKey, CancellationToken ct = default);

        Task DeleteFileAsync(string storageKey, CancellationToken ct = default);
    }
}
