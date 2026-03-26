using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Settings.Queries.GetMyAccount
{
    public class GetMyAccountSettingsQuery : IGetMyAccountSettingsQuery
    {
        private readonly UserManager<UsuarioEntity> _userManager;

        public GetMyAccountSettingsQuery(UserManager<UsuarioEntity> userManager)
        {
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(int userId, CancellationToken ct = default)
        {
            if (userId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "UserId inválido");

            var user = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId, ct);

            if (user == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Usuario no encontrado");

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
