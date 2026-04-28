using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Curso.Queries.GetMyCursos.Profesor
{
    public class GetMyCursosProfesorQuery : IGetMyCursosProfesorQuery
    {
        private readonly IDataBaseService _db;

        public GetMyCursosProfesorQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int userId, int pageNumber, int pageSize, string? search, int? anio, int? estado)
        {
            if (userId <= 0)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "Usuario inválido");

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            // Confirmar que el usuario sea profesor (PK compartida)
            var existeProfesor = await _db.Profesores.AsNoTracking().AnyAsync(p => p.Id == userId);
            if (!existeProfesor)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No sos Profesor");

            var q = _db.Cursos
                .AsNoTracking()
                .Where(c => c.Profesores.Any(cp => cp.ProfesorId == userId))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                q = q.Where(c => c.Nombre.ToLower().Contains(s));
            }

            if (anio.HasValue)
                q = q.Where(c => c.Anio == anio.Value);

            if (estado.HasValue)
            {
                if (estado.Value < 1 || estado.Value > 3)
                    return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Estado inválido");

                q = q.Where(c => (int)c.Estado == estado.Value);
            }

            var total = await q.CountAsync();

            var items = await q
                .OrderByDescending(c => c.Anio)
                .ThenBy(c => c.Nombre)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CursoResumenModel
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Anio = c.Anio,
                    Estado = c.Estado,
                    CantidadHorarios = c.Horarios.Count
                })
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
