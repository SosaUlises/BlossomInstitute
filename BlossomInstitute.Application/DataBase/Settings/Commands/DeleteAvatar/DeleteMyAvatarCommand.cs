using BlossomInstitute.Application.DataBase.CloudinaryService.Commands.UploadFile;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Settings.Commands.DeleteAvatar
{
    public class DeleteMyAvatarCommand : IDeleteMyAvatarCommand
    {
        private readonly UserManager<UsuarioEntity> _userManager;
        private readonly IFileStorageService _fileStorageService;

        public DeleteMyAvatarCommand(
            UserManager<UsuarioEntity> userManager,
            IFileStorageService fileStorageService)
        {
            _userManager = userManager;
            _fileStorageService = fileStorageService;
        }

        public async Task<BaseResponseModel> Execute(
            int userId,
            CancellationToken ct = default)
        {
            if (userId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "UserId inválido");

            var user = await _userManager.Users
                .FirstOrDefaultAsync(x => x.Id == userId, ct);

            if (user == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Usuario no encontrado");

            var previousAvatarPublicId = user.AvatarPublicId;

            user.AvatarUrl = null;
            user.AvatarPublicId = null;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    updateResult.Errors.Select(e => e.Description).ToList());
            }

            if (!string.IsNullOrWhiteSpace(previousAvatarPublicId))
            {
                try
                {
                    await _fileStorageService.DeleteFileAsync(previousAvatarPublicId, ct);
                }
                catch
                {
                    return ResponseApiService.Response(
                        StatusCodes.Status502BadGateway,
                        message: "La foto se quitó del perfil, pero no se pudo eliminar de Cloudinary");
                }
            }

            return ResponseApiService.Response(
                StatusCodes.Status200OK,
                new { },
                "Foto de perfil eliminada.");
        }
    }
}
