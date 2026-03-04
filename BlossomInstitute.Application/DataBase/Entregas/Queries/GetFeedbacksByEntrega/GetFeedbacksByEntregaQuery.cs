using BlossomInstitute.Application.DataBase.Entregas.Queries.Models;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Entregas.Queries.GetFeedbacksByEntrega
{
    public class GetFeedbacksByEntregaQuery : IGetFeedbacksByEntregaQuery
    {
        private readonly IDataBaseService _db;

        public GetFeedbacksByEntregaQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int cursoId, int tareaId, int alumnoId, int profesorUserId, CancellationToken ct)
        {

            if (cursoId <= 0 || tareaId <= 0 || alumnoId <= 0) return ResponseApiService.Response(400, "Parámetros inválidos");

            var profAsignado = await _db.CursoProfesores.AsNoTracking()
                .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);
            if (!profAsignado) return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Profesor no asignado a este curso");

            var tareaOk = await _db.Tareas.AsNoTracking()
                .AnyAsync(t => t.Id == tareaId && t.CursoId == cursoId, ct);
            if (!tareaOk) return ResponseApiService.Response(StatusCodes.Status404NotFound, "Tarea no encontrada");

            var entregaId = await _db.Entregas.AsNoTracking()
                .Where(e => e.TareaId == tareaId && e.AlumnoId == alumnoId)
                .Select(e => e.Id)
                .FirstOrDefaultAsync(ct);

            if (entregaId == 0) return ResponseApiService.Response(StatusCodes.Status404NotFound, "Entrega no encontrada");

            var feedbacks = await _db.EntregaFeedbacks.AsNoTracking()
                .Where(f => f.EntregaId == entregaId)
                .OrderByDescending(f => f.FechaCorreccionUtc)
                .Select(f => new FeedbackHistoryItemModel
                {
                    FeedbackId = f.Id,
                    EsVigente = f.EsVigente,
                    Estado = (int)f.Estado,
                    Nota = f.Nota,
                    Comentario = f.Comentario,
                    FechaCorreccionUtc = f.FechaCorreccionUtc,
                    ArchivoCorregidoUrl = f.ArchivoCorregidoUrl,
                    ArchivoCorregidoNombre = f.ArchivoCorregidoNombre
                })
                .ToListAsync(ct);

            return ResponseApiService.Response(StatusCodes.Status200OK, feedbacks);
        }
    }
}
