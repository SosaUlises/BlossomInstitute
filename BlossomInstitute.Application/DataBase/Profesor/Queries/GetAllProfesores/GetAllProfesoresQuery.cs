using AutoMapper;
using AutoMapper.QueryableExtensions;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Profesor.Queries.GetAllProfesores
{
    public class GetAllProfesoresQuery : IGetAllProfesoresQuery
    {
        private readonly IDataBaseService _db;
        private readonly IConfigurationProvider _mapperConfig;

        public GetAllProfesoresQuery(IDataBaseService db, IMapper mapper)
        {
            _db = db;
            _mapperConfig = mapper.ConfigurationProvider;
        }

        public async Task<BaseResponseModel> Execute(int pageNumber, int pageSize)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var roleId = await _db.Roles
                .Where(r => r.Name == "Profesor")
                .Select(r => r.Id)
                .SingleOrDefaultAsync();

            if (roleId == 0)
                return ResponseApiService.Response(StatusCodes.Status500InternalServerError, "Rol Profesor no existe");

            var query =
                from u in _db.Usuarios
                join ur in _db.UserRoles on u.Id equals ur.UserId
                where ur.RoleId == roleId
                select u;

            query = query.Where(u => u.Activo);

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(u => u.Apellido)
                .ThenBy(u => u.Nombre)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<GetAllProfesoresModel>(_mapperConfig)
                .ToListAsync();

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                pageNumber,
                pageSize,
                total,
                items
            });
        }
    }
}
