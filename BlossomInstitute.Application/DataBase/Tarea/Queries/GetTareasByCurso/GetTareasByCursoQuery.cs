using BlossomInstitute.Application.DataBase.Tarea.Queries.Models;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Tarea;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Tarea.Queries.GetTareasByCurso
{
    public class GetTareasByCursoQuery : IGetTareasByCursoQuery
    {
        private readonly IDataBaseService _db;

        public GetTareasByCursoQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            EstadoTarea? estado,
            int pageNumber,
            int pageSize,
            string? search = null,
            CancellationToken ct = default)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(400, "CursoId inválido");

            if (pageNumber <= 0)
                pageNumber = 1;

            if (pageSize <= 0)
                pageSize = 10;

            if (pageSize > 200)
                pageSize = 200;

            var query = _db.Tareas
                .AsNoTracking()
                .Where(t => t.CursoId == cursoId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTrimmed = search.Trim();

                query = query.Where(t =>
                    t.Titulo.Contains(searchTrimmed) ||
                    (t.Consigna != null && t.Consigna.Contains(searchTrimmed)));
            }

            if (estado.HasValue)
                query = query.Where(t => t.Estado == estado.Value);

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(t => t.CreatedAtUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TareaByCursoItemModel
                {
                    Id = t.Id,
                    CursoId = t.CursoId,
                    ProfesorId = t.ProfesorId,
                    Titulo = t.Titulo,
                    Estado = (int)t.Estado,
                    FechaEntregaUtc = t.FechaEntregaUtc,
                    CreatedAtUtc = t.CreatedAtUtc
                })
                .ToListAsync(ct);

            var response = new TareasByCursoPagedModel
            {
                Total = total,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = items
            };

            return ResponseApiService.Response(StatusCodes.Status200OK, response);
        }
    }
}

