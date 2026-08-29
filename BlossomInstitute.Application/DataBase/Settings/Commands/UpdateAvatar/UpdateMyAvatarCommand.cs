using BlossomInstitute.Application.External.Archivos;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Settings.Commands.UpdateAvatar
{
    public class UpdateMyAvatarCommand : IUpdateMyAvatarCommand
    {
        private readonly UserManager<UsuarioEntity> _userManager;
        private readonly IAlmacenamientoArchivoService _almacenamientoArchivoService;

        public UpdateMyAvatarCommand(
            UserManager<UsuarioEntity> userManager,
            IAlmacenamientoArchivoService almacenamientoArchivoService)
        {
            _userManager = userManager;
            _almacenamientoArchivoService = almacenamientoArchivoService;
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

            ArchivoSubidoResponseModel upload;
            try
            {
                upload = await _almacenamientoArchivoService.SubirAvatarAsync(model.File, ct);
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
                await _almacenamientoArchivoService.EliminarImagenAsync(publicId, ct);
            }
            catch
            {
            }
        }
    }
}
