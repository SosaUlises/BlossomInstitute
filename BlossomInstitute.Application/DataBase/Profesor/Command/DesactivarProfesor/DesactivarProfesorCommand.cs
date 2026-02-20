using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace BlossomInstitute.Application.DataBase.Profesor.Command.DeleteProfesor
{
    public class DesactivarProfesorCommand : IDesactivarProfesorCommand
    {
        private readonly UserManager<UsuarioEntity> _userManager;

        public DesactivarProfesorCommand(UserManager<UsuarioEntity> userManager)
        {
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Profesor no encontrado");

            if (!await _userManager.IsInRoleAsync(user, "Profesor"))
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Profesor no encontrado");

            if (await _userManager.IsInRoleAsync(user, "Administrador"))
                return ResponseApiService.Response(StatusCodes.Status409Conflict, "No se puede desactivar un Administrador");

            if (!user.Activo)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "El profesor ya está desactivado");

            user.Activo = false;

            var updateRes = await _userManager.UpdateAsync(user);
            if (!updateRes.Succeeded)
                return ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    updateRes.Errors.Select(e => e.Description).ToList(),
                    "Error al desactivar el profesor");

            // Invalida tokens existentes
            await _userManager.UpdateSecurityStampAsync(user);

            return ResponseApiService.Response(StatusCodes.Status200OK, "Profesor desactivado correctamente");
        }
    }
}

