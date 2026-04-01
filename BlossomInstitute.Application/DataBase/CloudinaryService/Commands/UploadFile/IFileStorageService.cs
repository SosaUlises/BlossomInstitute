using Microsoft.AspNetCore.Http;

namespace BlossomInstitute.Application.DataBase.CloudinaryService.Commands.UploadFile
{
    public interface IFileStorageService
    {
        Task<UploadFileResponseModel> UploadAsync(
            IFormFile file,
            string folder,
            CancellationToken ct = default);
    }
}
