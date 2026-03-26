using BlossomInstitute.Application.DataBase.Settings.Queries;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Settings.Command.UpdateAccount
{
    public class UpdateMyAccountSettingsCommand : IUpdateMyAccountSettingsCommand
    {
        private readonly UserManager<UsuarioEntity> _userManager;

        public UpdateMyAccountSettingsCommand(UserManager<UsuarioEntity> userManager)
        {
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(
            int userId,
            UpdateMyAccountSettingsModel model,
            CancellationToken ct = default)
        {
            if (userId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "UserId inválido");

            var user = await _userManager.Users
                .FirstOrDefaultAsync(x => x.Id == userId, ct);

            if (user == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Usuario no encontrado");

            var normalizedEmail = _userManager.NormalizeEmail(model.Email.Trim());

            var emailEnUso = await _userManager.Users
                .AsNoTracking()
                .AnyAsync(x => x.Id != userId && x.NormalizedEmail == normalizedEmail, ct);

            if (emailEnUso)
                return ResponseApiService.Response(StatusCodes.Status409Conflict, "El email ya está en uso");

            var dniEnUso = await _userManager.Users
                .AsNoTracking()
                .AnyAsync(x => x.Id != userId && x.Dni == model.Dni, ct);

            if (dniEnUso)
                return ResponseApiService.Response(StatusCodes.Status409Conflict, "El DNI ya está en uso");

            user.Nombre = model.Nombre.Trim();
            user.Apellido = model.Apellido.Trim();
            user.Email = model.Email.Trim();
            user.UserName = model.Email.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.Telefono) ? null : model.Telefono.Trim();
            user.Dni = model.Dni;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    result.Errors.Select(e => e.Description).ToList());
            }

            var roles = await _userManager.GetRolesAsync(user);

            var response = new GetMyAccountSettingsModel
            {
                Id = user.Id,
                Nombre = user.Nombre,
                Apellido = user.Apellido,
                Email = user.Email ?? "",
                Telefono = user.PhoneNumber,
                Dni = user.Dni,
                Activo = user.Activo,
                Roles = roles.ToList()
            };

            return ResponseApiService.Response(StatusCodes.Status200OK, response);
        }
    }
}

