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
        private readonly IMapper _mapper;

        public GetProfesorByIdQuery(UserManager<UsuarioEntity> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<BaseResponseModel> Execute(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Profesor no encontrado");

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Profesor"))
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Profesor no encontrado");

            return ResponseApiService.Response(StatusCodes.Status200OK, _mapper.Map<GetProfesorModel>(user));
        }
    }
}
