using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace BlossomInstitute.Application.DataBase.Alumno.Commands.ActivarAlumno
{
    public class ActivarAlumnoCommand : IActivarAlumnoCommand
    {
        private readonly UserManager<UsuarioEntity> _userManager;

        public ActivarAlumnoCommand(UserManager<UsuarioEntity> userManager)
        {
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(int userId)
        {
            if (userId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Id inválido");

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Alumno no encontrado");

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Administrador"))
                return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "No se puede activar a un Administrador");

            if (!roles.Contains("Alumno"))
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Alumno no encontrado");

            if (user.Activo)
                return ResponseApiService.Response(StatusCodes.Status200OK, message: "Alumno ya estaba activado");

            user.Activo = true;

            var updateRes = await _userManager.UpdateAsync(user);
            if (!updateRes.Succeeded)
                return ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    updateRes.Errors.Select(e => e.Description).ToList(),
                    "Error al activar al alumno");

            return ResponseApiService.Response(StatusCodes.Status200OK, message: "Alumno activado correctamente");
        }
    }
}
