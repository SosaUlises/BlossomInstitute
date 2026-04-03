using AutoMapper;
using BlossomInstitute.Application.DataBase.Profesor.Queries.GetAllProfesores;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace BlossomInstitute.Application.DataBase.Profesor.Queries.GetById
{
    public class GetProfesorByIdQuery : IGetProfesorByIdQuery
    {
        private readonly UserManager<UsuarioEntity> _userManager;

        public GetProfesorByIdQuery(UserManager<UsuarioEntity> userManager)
        {
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return ResponseApiService.Response(
                    StatusCodes.Status404NotFound,
                    "Profesor no encontrado"
                );

            var roles = await _userManager.GetRolesAsync(user);

            if (!roles.Contains("Profesor"))
                return ResponseApiService.Response(
                    StatusCodes.Status404NotFound,
                    "Profesor no encontrado"
                );

            var model = new GetProfesorModel
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
