using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Alumno.Commands.UpdateAlumno
{
    public class UpdateAlumnoCommand : IUpdateAlumnoCommand
    {
        private readonly UserManager<UsuarioEntity> _userManager;
        private readonly IDataBaseService _dataBaseService;

        public UpdateAlumnoCommand(
            UserManager<UsuarioEntity> userManager,
            IDataBaseService dataBaseService)
        {
            _userManager = userManager;
            _dataBaseService = dataBaseService;
        }

        public async Task<BaseResponseModel> Execute(int userId, UpdateAlumnoModel model)
        {
            if (userId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Id invalido");

            if (model == null)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Datos del alumno invalidos");

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Alumno no encontrado");

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Administrador"))
                return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "No se puede actualizar a un Administrador");

            if (!roles.Contains("Alumno"))
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Alumno no encontrado");

            var email = model.Email?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email))
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Email invalido");

            var normalizedEmail = _userManager.NormalizeEmail(email);
            var existeEmail = await _userManager.Users
                .AnyAsync(x => x.NormalizedEmail == normalizedEmail && x.Id != user.Id);
            if (existeEmail)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: $"Ya existe un usuario con el email {email}");

            var existeDni = await _userManager.Users.AnyAsync(x => x.Dni == model.Dni && x.Id != user.Id);
            if (existeDni)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: $"Ya existe un usuario con el DNI {model.Dni}");

            await using var tx = await _dataBaseService.BeginTransactionAsync();

            try
            {
                user.Nombre = model.Nombre;
                user.Apellido = model.Apellido;
                user.Dni = model.Dni;
                user.PhoneNumber = model.Telefono?.Trim();

                var setEmailResult = await _userManager.SetEmailAsync(user, email);
                if (!setEmailResult.Succeeded)
                {
                    await tx.RollbackAsync();
                    return ResponseApiService.Response(
                        StatusCodes.Status400BadRequest,
                        setEmailResult.Errors.Select(e => e.Description).ToList(),
                        "Error al actualizar email");
                }

                var setUserNameResult = await _userManager.SetUserNameAsync(user, email);
                if (!setUserNameResult.Succeeded)
                {
                    await tx.RollbackAsync();
                    return ResponseApiService.Response(
                        StatusCodes.Status400BadRequest,
                        setUserNameResult.Errors.Select(e => e.Description).ToList(),
                        "Error al actualizar username");
                }

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    await tx.RollbackAsync();
                    return ResponseApiService.Response(
                        StatusCodes.Status400BadRequest,
                        updateResult.Errors.Select(e => e.Description).ToList(),
                        "Error al actualizar al alumno");
                }

                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                    if (!passwordResult.Succeeded)
                    {
                        await tx.RollbackAsync();
                        return ResponseApiService.Response(
                            StatusCodes.Status400BadRequest,
                            passwordResult.Errors.Select(e => e.Description).ToList(),
                            "Error al actualizar contrasena");
                    }
                }

                await tx.CommitAsync();

                return ResponseApiService.Response(StatusCodes.Status200OK, message: "Alumno actualizado correctamente");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
