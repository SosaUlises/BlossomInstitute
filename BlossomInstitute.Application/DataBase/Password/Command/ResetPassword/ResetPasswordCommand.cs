using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Net;

namespace BlossomInstitute.Application.DataBase.Password.Command.ResetPassword
{
    public class ResetPasswordCommand : IResetPasswordCommand
    {
        private readonly UserManager<UsuarioEntity> _userManager;

        public ResetPasswordCommand(UserManager<UsuarioEntity> userManager)
        {
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(ResetPasswordModel model)
        {
            var email = model.Email?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(model.Token) ||
                string.IsNullOrWhiteSpace(model.NewPassword) ||
                string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "Email, token y contraseñas son obligatorios");
            }

            if (model.NewPassword != model.ConfirmPassword)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "Las contraseñas no coinciden");

            var usuario = await _userManager.FindByEmailAsync(email);
            if (usuario == null)
            {
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "Token inválido o expirado");
            }

            if (!usuario.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Usuario inactivo");


            var decodedToken = WebUtility.UrlDecode(model.Token)?.Trim();
            if (string.IsNullOrWhiteSpace(decodedToken))
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "Token inválido o expirado");


            var result = await _userManager.ResetPasswordAsync(usuario, decodedToken, model.NewPassword);

            if (!result.Succeeded)
            {
                return ResponseApiService.Response(StatusCodes.Status400BadRequest,
                    result.Errors.Select(e => e.Description).ToList(),
                    "No se pudo restablecer la contraseña");
            }

            return ResponseApiService.Response(StatusCodes.Status200OK, new { }, "Contraseña restablecida correctamente");
        }
    }
}
