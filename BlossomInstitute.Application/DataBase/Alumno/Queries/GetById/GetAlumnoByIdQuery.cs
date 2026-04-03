using BlossomInstitute.Application.DataBase.Alumno.Queries.GetAll;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace BlossomInstitute.Application.DataBase.Alumno.Queries.GetById
{
    public class GetAlumnoByIdQuery : IGetAlumnoByIdQuery
    {
        private readonly UserManager<UsuarioEntity> _userManager;

        public GetAlumnoByIdQuery(UserManager<UsuarioEntity> userManager)
        {
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return ResponseApiService.Response(
                    StatusCodes.Status404NotFound,
                    "Alumno no encontrado"
                );

            var roles = await _userManager.GetRolesAsync(user);

            if (!roles.Contains("Alumno"))
                return ResponseApiService.Response(
                    StatusCodes.Status404NotFound,
                    "Alumno no encontrado"
                );

            var model = new GetAlumnoModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Nombre = user.Nombre ?? string.Empty,
                Apellido = user.Apellido ?? string.Empty,
                Dni = user.Dni,
                Telefono = user.PhoneNumber ?? string.Empty,
                Activo = user.Activo
            };

            return ResponseApiService.Response(
                StatusCodes.Status200OK,
                model
            );
        }
    }
}
