using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace BlossomInstitute.Application.DataBase.Alumno.Commands.DesactivarAlumno
{
    public class DesactivarAlumnoCommand : IDesactivarAlumnoCommand
    {
        private readonly UserManager<UsuarioEntity> _userManager;

        public DesactivarAlumnoCommand(UserManager<UsuarioEntity> userManager)
        {
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Alumno no encontrado");

            if (!await _userManager.IsInRoleAsync(user, "Alumno"))
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Alumno no encontrado");

            if (await _userManager.IsInRoleAsync(user, "Administrador"))
                return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "No se puede desactivar un Administrador");

            if (!user.Activo)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "El alumno ya está desactivado");

            user.Activo = false;

            var updateRes = await _userManager.UpdateAsync(user);
            if (!updateRes.Succeeded)
                return ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    updateRes.Errors.Select(e => e.Description).ToList(),
                    "Error al desactivar al alumno");

            // Invalida tokens existentes
            await _userManager.UpdateSecurityStampAsync(user);

            return ResponseApiService.Response(StatusCodes.Status200OK, message: "Alumno desactivado correctamente");
        }
    }
}

