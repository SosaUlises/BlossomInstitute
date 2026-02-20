using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Profesor.Command.UpdateProfesor
{
    public class UpdateProfesorCommand : IUpdateProfesorCommand
    {
        private readonly UserManager<UsuarioEntity> _userManager;
        private readonly IDataBaseService _dataBaseService;

        public UpdateProfesorCommand(
            UserManager<UsuarioEntity> userManager,
            IDataBaseService dataBaseService)
        {
            _userManager = userManager;
            _dataBaseService = dataBaseService;
        }

        public async Task<BaseResponseModel> Execute(int userId, UpdateProfesorModel model)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Profesor no encontrado");

            if (!await _userManager.IsInRoleAsync(user, "Profesor"))
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "El usuario no es Profesor");

            var email = model.Email?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email))
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "Email inválido");

            var normalizedEmail = _userManager.NormalizeEmail(email);
            var existeEmail = await _userManager.Users
                .AnyAsync(x => x.NormalizedEmail == normalizedEmail && x.Id != user.Id);
            if (existeEmail)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, $"Ya existe un usuario con el email {email}");

            var existeDni = await _userManager.Users.AnyAsync(x => x.Dni == model.Dni && x.Id != user.Id);
            if (existeDni)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, $"Ya existe un usuario con el DNI {model.Dni}");

            await using var tx = await _dataBaseService.BeginTransactionAsync();

            user.Nombre = model.Nombre;
            user.Apellido = model.Apellido;
            user.Dni = model.Dni;
            user.PhoneNumber = model.Telefono?.Trim();

            var setEmailRes = await _userManager.SetEmailAsync(user, email);
            if (!setEmailRes.Succeeded)
            {
                await tx.RollbackAsync();
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, setEmailRes.Errors, "Error al actualizar email");
            }

            var setUserNameRes = await _userManager.SetUserNameAsync(user, email);
            if (!setUserNameRes.Succeeded)
            {
                await tx.RollbackAsync();
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, setUserNameRes.Errors, "Error al actualizar username");
            }

            var updRes = await _userManager.UpdateAsync(user);
            if (!updRes.Succeeded)
            {
                await tx.RollbackAsync();
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, updRes.Errors, "Error al actualizar al profesor");
            }

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passRes = await _userManager.ResetPasswordAsync(user, token, model.Password);
                if (!passRes.Succeeded)
                {
                    await tx.RollbackAsync();
                    return ResponseApiService.Response(StatusCodes.Status400BadRequest, passRes.Errors, "Error al actualizar contraseña");
                }
            }

            await tx.CommitAsync();
            return ResponseApiService.Response(StatusCodes.Status200OK, "Profesor actualizado correctamente");
        }
    }
}
