using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Curso.Queries.GetAllCursos
{
    public class GetAllCursosQuery : IGetAllCursosQuery
    {
        private readonly IDataBaseService _db;

        public GetAllCursosQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(
            int pageNumber,
            int pageSize,
            string? search,
            int? anio,
            int? estado)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var query = _db.Cursos
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                query = query.Where(c => c.Nombre.ToLower().Contains(s));
            }

            if (anio.HasValue)
            {
                query = query.Where(c => c.Anio == anio.Value);
            }

            if (estado.HasValue)
            {
                if (estado.Value < 1 || estado.Value > 3)
                    return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Estado inválido");

                query = query.Where(c => (int)c.Estado == estado.Value);
            }

            var total = await query.CountAsync();

            var data = await query
                .OrderByDescending(c => c.Anio)
                .ThenBy(c => c.Nombre)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new GetAllCursosModel
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Anio = c.Anio,
                    Estado = c.Estado,
                    CantidadHorarios = c.Horarios.Count,
                    CantidadProfesores = c.Profesores.Count,
                    CantidadAlumnos = c.Matriculas.Count
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
