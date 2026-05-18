using BlossomInstitute.Application.DataBase.CloudinaryService.Commands.UploadFile;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Settings.Command.UpdateAvatar
{
    public class UpdateMyAvatarCommand : IUpdateMyAvatarCommand
    {
        private readonly UserManager<UsuarioEntity> _userManager;
        private readonly IFileStorageService _fileStorageService;

        public UpdateMyAvatarCommand(
            UserManager<UsuarioEntity> userManager,
            IFileStorageService fileStorageService)
        {
            _userManager = userManager;
            _fileStorageService = fileStorageService;
        }

        public async Task<BaseResponseModel> Execute(
            int userId,
            UpdateAvatarRequest model,
            CancellationToken ct = default)
        {
            if (userId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "UserId inválido");

            var user = await _userManager.Users
                .FirstOrDefaultAsync(x => x.Id == userId, ct);

            if (user == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Usuario no encontrado");

            UploadFileResponseModel upload;
            try
            {
                upload = await _fileStorageService.UploadAvatarAsync(model.File, ct);
            }
            catch
            {
                return ResponseApiService.Response(
                    StatusCodes.Status502BadGateway,
                    message: "No se pudo subir la foto de perfil");
            }

            var previousAvatarPublicId = user.AvatarPublicId;

            user.AvatarUrl = upload.Url;
            user.AvatarPublicId = upload.StorageKey;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                await TryDeleteAvatarAsync(upload.StorageKey, ct);

                return ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    updateResult.Errors.Select(e => e.Description).ToList());
            }

            if (!string.IsNullOrWhiteSpace(previousAvatarPublicId))
                await TryDeleteAvatarAsync(previousAvatarPublicId, ct);

            return ResponseApiService.Response(
                StatusCodes.Status200OK,
                new UpdateAvatarResponseModel
                {
                    AvatarUrl = user.AvatarUrl
                },
                "Foto de perfil actualizada.");
        }

        private async Task TryDeleteAvatarAsync(string publicId, CancellationToken ct)
        {
            try
            {
                await _fileStorageService.DeleteFileAsync(publicId, ct);
            }
            catch
            {
            }
        }
    }
}
