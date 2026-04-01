using Microsoft.AspNetCore.Http;

namespace BlossomInstitute.Application.DataBase.CloudinaryService.Commands.UploadFile
{
    public class UploadFileRequest
    {
        public IFormFile File { get; set; } = default!;
        public string? Folder { get; set; }
    }
    public class DeleteUploadRequest
    {
        public string StorageKey { get; set; } = default!;
    }
}
