using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Settings.Command.ChangePassword
{
    public class ChangeMyPasswordCommand : IChangeMyPasswordCommand
    {
        private readonly UserManager<UsuarioEntity> _userManager;

        public ChangeMyPasswordCommand(UserManager<UsuarioEntity> userManager)
        {
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(
            int userId,
            ChangeMyPasswordModel model,
            CancellationToken ct = default)
        {
            if (userId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "UserId inválido");

            var user = await _userManager.Users
                .FirstOrDefaultAsync(x => x.Id == userId, ct);

            if (user == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Usuario no encontrado");

            if (!user.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "El usuario está inactivo");

            var result = await _userManager.ChangePasswordAsync(
                user,
                model.CurrentPassword,
                model.NewPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .Select(x => x.Description)
                    .ToList();

                return ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    errors);
            }

            return ResponseApiService.Response(
                StatusCodes.Status200OK,
                "La contraseña fue actualizada correctamente");
        }
    }
}

