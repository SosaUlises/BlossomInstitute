using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Clase.Queries.GetClasesByCurso
{
    public class GetClasesByCursoQuery : IGetClasesByCursoQuery
    {
        private readonly IDataBaseService _db;

        public GetClasesByCursoQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            DateOnly? from,
            DateOnly? to,
            int pageNumber,
            int pageSize,
            CancellationToken ct = default)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "CursoId inválido");

            var existeCurso = await _db.Cursos.AnyAsync(c => c.Id == cursoId, ct);
            if (!existeCurso)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Curso no encontrado");

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 200) pageSize = 200;

            var query = _db.Clases.AsNoTracking().Where(x => x.CursoId == cursoId);

            if (from.HasValue) query = query.Where(x => x.Fecha >= from.Value);
            if (to.HasValue) query = query.Where(x => x.Fecha <= to.Value);

            var total = await query.CountAsync(ct);

            var clases = await query
                .OrderByDescending(x => x.Fecha)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Id,
                    x.CursoId,
                    x.Fecha,
                    x.Estado,
                    x.Descripcion
                })
                .ToListAsync(ct);

            var claseIds = clases.Select(x => x.Id).ToList();

            // Agregados de asistencia (si no hay clases, evita query)
            var stats = claseIds.Count == 0
             ? new List<ClaseAsistenciaStats>()
             : await _db.Asistencias.AsNoTracking()
                 .Where(a => claseIds.Contains(a.ClaseId))
                 .GroupBy(a => a.ClaseId)
                 .Select(g => new ClaseAsistenciaStats
                 {
                     ClaseId = g.Key,
                     Cant = g.Count(),
                     Presentes = g.Count(x => x.Estado == EstadoAsistencia.Presente),
                     Ausentes = g.Count(x => x.Estado == EstadoAsistencia.Ausente),
                 })
            .ToListAsync(ct);

            var statsByClase = stats.ToDictionary(x => x.ClaseId);


            var data = clases.Select(c =>
            {
                statsByClase.TryGetValue(c.Id, out var s);
                return new ClaseResumenModel
                {
                    Id = c.Id,
                    CursoId = c.CursoId,
                    Fecha = c.Fecha.ToString("yyyy-MM-dd"),
                    Estado = c.Estado,
                    Descripcion = c.Descripcion,
                    CantAsistencias = s?.Cant ?? 0,
                    CantPresentes = s?.Presentes ?? 0,
                    CantAusentes = s?.Ausentes ?? 0
                };
            }).ToList();

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                total,
                pageNumber,
                pageSize,
                items = data
            });
        }

        private sealed class ClaseAsistenciaStats
        {
            public int ClaseId { get; set; }
            public int Cant { get; set; }
            public int Presentes { get; set; }
            public int Ausentes { get; set; }
        }
    }
}
