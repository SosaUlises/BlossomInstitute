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
        private readonly IDataBaseService _dataBaseService;

        public DesactivarAlumnoCommand(
            UserManager<UsuarioEntity> userManager,
            IDataBaseService dataBaseService)
        {
            _userManager = userManager;
            _dataBaseService = dataBaseService;
        }

        public async Task<BaseResponseModel> Execute(int userId)
        {
            if (userId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Id invalido");

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Alumno no encontrado");

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Administrador"))
                return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "No se puede desactivar a un Administrador");

            if (!roles.Contains("Alumno"))
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Alumno no encontrado");

            if (!user.Activo)
                return ResponseApiService.Response(StatusCodes.Status200OK, message: "Alumno ya estaba desactivado");

            await using var tx = await _dataBaseService.BeginTransactionAsync();

            try
            {
                user.Activo = false;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    await tx.RollbackAsync();
                    return ResponseApiService.Response(
                        StatusCodes.Status400BadRequest,
                        updateResult.Errors.Select(e => e.Description).ToList(),
                        "Error al desactivar al alumno");
                }

                var securityStampResult = await _userManager.UpdateSecurityStampAsync(user);
                if (!securityStampResult.Succeeded)
                {
                    await tx.RollbackAsync();
                    return ResponseApiService.Response(
                        StatusCodes.Status400BadRequest,
                        securityStampResult.Errors.Select(e => e.Description).ToList(),
                        "Error al invalidar las sesiones del alumno");
                }

                await tx.CommitAsync();

                return ResponseApiService.Response(StatusCodes.Status200OK, message: "Alumno desactivado correctamente");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
