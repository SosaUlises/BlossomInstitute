using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.Alumno.Queries.GetAsignableByCurso
{
    public class GetAsignableAlumnosByCursoQuery : IGetAsignableAlumnosByCursoQuery
    {
        private readonly IDataBaseService _db;

        public GetAsignableAlumnosByCursoQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int cursoId, int pageNumber, int pageSize, string? search)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            search = search?.Trim();

            var curso = await _db.Cursos
                .AsNoTracking()
                .Where(x => x.Id == cursoId)
                .Select(x => new
                {
                    x.Id,
                    x.Anio
                })
                .FirstOrDefaultAsync();

            if (curso == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Curso no encontrado");

            var rolAlumnoId = await _db.Roles
                .Where(r => r.Name == "Alumno")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (rolAlumnoId == 0)
                return ResponseApiService.Response(StatusCodes.Status500InternalServerError, message: "Rol Alumno no existe");

            var alumnosYaMatriculadosEnEseAnio = await _db.Matriculas
                .AsNoTracking()
                .Where(m => m.Curso.Anio == curso.Anio)
                .Select(m => m.AlumnoId)
                .Distinct()
                .ToListAsync();

            var query = from u in _db.Usuarios.AsNoTracking()
                        join ur in _db.UserRoles.AsNoTracking() on u.Id equals ur.UserId
                        where ur.RoleId == rolAlumnoId
                              && u.Activo
                              && !alumnosYaMatriculadosEnEseAnio.Contains(u.Id)
                        select u;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLowerInvariant();
                query = query.Where(u =>
                    (u.Nombre ?? "").ToLower().Contains(s) ||
                    (u.Apellido ?? "").ToLower().Contains(s) ||
                    (u.Email ?? "").ToLower().Contains(s) ||
                    u.Dni.ToString().Contains(s));
            }

            var total = await query.CountAsync();

            var data = await query
                .OrderBy(u => u.Apellido)
                .ThenBy(u => u.Nombre)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new GetAsignableAlumnoModel
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
