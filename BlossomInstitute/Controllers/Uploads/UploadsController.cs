using BlossomInstitute.Application.DataBase.CloudinaryService.Commands.UploadFile;
using BlossomInstitute.Common.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlossomInstitute.Controllers.Uploads
{

    [ApiController]
    [Route("api/v1/uploads")]
    [Authorize(Roles = "Profesor,Administrador,Alumno")]
    public class UploadsController : ControllerBase
    {
        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(25_000_000)]
        public async Task<IActionResult> Upload(
            [FromForm] UploadFileRequest request,
            [FromServices] IFileStorageService storage,
            CancellationToken ct)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest(ResponseApiService.Response(400, "Debe adjuntar un archivo"));

            var allowedExtensions = new[]
            {
                ".pdf", ".doc", ".docx", ".xls", ".xlsx",
                ".ppt", ".pptx", ".txt", ".jpg", ".jpeg",
                ".png", ".webp", ".mp3", ".mp4", ".zip", ".rar"
            };

            var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                return BadRequest(ResponseApiService.Response(400, "Tipo de archivo no permitido"));

            var result = await storage.UploadAsync(
                request.File,
                request.Folder ?? "general",
                ct
            );

            return StatusCode(
                201,
                ResponseApiService.Response(201, result, "Archivo subido correctamente")
            );
        }
    }
}

