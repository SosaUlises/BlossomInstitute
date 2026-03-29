using BlossomInstitute.Application.DataBase.Tarea.Queries.Models;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Tarea.Queries.GetTareasById
{
    public class GetTareaByIdQuery : IGetTareaByIdQuery
    {
        private readonly IDataBaseService _db;

        public GetTareaByIdQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int cursoId, int tareaId, CancellationToken ct = default)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(400, "CursoId inválido");

            if (tareaId <= 0)
                return ResponseApiService.Response(400, "TareaId inválido");

            var tarea = await _db.Tareas
                .AsNoTracking()
                .Where(t => t.CursoId == cursoId && t.Id == tareaId)
                .Select(t => new TareaDetailModel
                {
                    Id = t.Id,
                    CursoId = t.CursoId,
                    ProfesorId = t.ProfesorId,
                    Titulo = t.Titulo,
                    Consigna = t.Consigna,
                    Estado = (int)t.Estado,
                    FechaEntregaUtc = t.FechaEntregaUtc,
                    CreatedAtUtc = t.CreatedAtUtc,
                    UpdatedAtUtc = t.UpdatedAtUtc,
                    Recursos = t.Recursos
                        .Select(r => new TareaRecursoItemModel
                        {
                            Id = r.Id,
                            Tipo = (int)r.Tipo,
                            Url = r.Url,
                            Nombre = r.Nombre
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(ct);

            if (tarea == null)
                return ResponseApiService.Response(404, "Tarea no encontrada");

            return ResponseApiService.Response(200, tarea);
        }
    }
}
