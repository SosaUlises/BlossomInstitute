using BlossomInstitute.Application.DataBase.Profesor.Queries.GetAllProfesores;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Alumno.Queries.GetAll
{
    public class GetAllAlumnosQuery : IGetAllAlumnosQuery
    {
        private readonly IDataBaseService _db;

        public GetAllAlumnosQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int pageNumber, int pageSize, string? search)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            search = search?.Trim();

            var rolAlumnoId = await _db.Roles
                .Where(r => r.Name == "Alumno")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (rolAlumnoId == 0)
                return ResponseApiService.Response(StatusCodes.Status500InternalServerError, message: "Rol Alumno no existe");

            // Usuarios que son Alumnos
            var query = from u in _db.Usuarios.AsNoTracking()
                        join ur in _db.UserRoles.AsNoTracking() on u.Id equals ur.UserId
                        where ur.RoleId == rolAlumnoId
                        select u;

            // Search (Nombre/Apellido/Email)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLowerInvariant();
                query = query.Where(u =>
                    (u.Nombre ?? "").ToLower().Contains(s) ||
                    (u.Apellido ?? "").ToLower().Contains(s) ||
                    (u.Email ?? "").ToLower().Contains(s));
            }

            var total = await query.CountAsync();

            var data = await query
                .OrderBy(u => u.Apellido)
                .ThenBy(u => u.Nombre)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new GetAlumnoModel
                {
                    Id = u.Id,
                    Email = u.Email!,
                    Nombre = u.Nombre!,
                    Apellido = u.Apellido!,
                    Dni = u.Dni,
                    Telefono = u.PhoneNumber ?? "",
                    Activo = u.Activo
                })
                .ToListAsync();

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                pageNumber,
                pageSize,
                total,
                items = data
            });
        }
    }
}
