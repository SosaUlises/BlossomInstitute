using BlossomInstitute.Application.DataBase.Entregas.Queries.Models;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Entregas.Queries.Alumno.GetMiEntregaByTarea
{
    public class GetMiEntregaByTareaQuery : IGetMiEntregaByTareaQuery
    {
        private readonly IDataBaseService _db;

        public GetMiEntregaByTareaQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int tareaId, int alumnoUserId, CancellationToken ct)
        {
            if (tareaId <= 0) return ResponseApiService.Response(400, message: "TareaId inválido");

            // Obtener curso de la tarea y validar matrícula (seguridad)
            var tarea = await _db.Tareas.AsNoTracking()
                .Where(t => t.Id == tareaId)
                .Select(t => new { t.Id, t.CursoId })
                .FirstOrDefaultAsync(ct);

            if (tarea == null) return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Tarea no encontrada");

            var matriculado = await _db.Matriculas.AsNoTracking()
                .AnyAsync(m => m.CursoId == tarea.CursoId && m.AlumnoId == alumnoUserId, ct);

            if (!matriculado) return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No estás matriculado en este curso");

            var entrega = await _db.Entregas.AsNoTracking()
                .Where(e => e.TareaId == tareaId && e.AlumnoId == alumnoUserId)
                .Select(e => new EntregaDetailModel
                {
                    EntregaId = e.Id,
                    TareaId = e.TareaId,
                    AlumnoId = e.AlumnoId,
                    Texto = e.Texto,
                    FechaEntregaUtc = e.FechaEntregaUtc,
                    EstadoEntrega = (int)e.Estado,
                    Adjuntos = e.Adjuntos
                        .OrderBy(a => a.Id)
                        .Select(a => new EntregaAdjuntoModel
                        {
                            Id = a.Id,
                            Tipo = a.Tipo,
                            Url = a.Url,
                            Nombre = a.Nombre
                        })
                        .ToList(),
                    FeedbackVigente = e.Feedbacks
                        .Where(f => f.EsVigente)
                        .OrderByDescending(f => f.FechaCorreccionUtc)
                        .Select(f => new FeedbackVigenteModel
                        {
                            FeedbackId = f.Id,
                            Estado = (int)f.Estado,
                            Comentario = f.Comentario,
                            Nota = f.Nota,
                            FechaCorreccionUtc = f.FechaCorreccionUtc,
                            Adjuntos = f.Adjuntos
                                .OrderBy(a => a.CreatedAtUtc)
                                .Select(a => new FeedbackAdjuntoItemModel
                                {
                                    Id = a.Id,
                                    Tipo = (int)a.Tipo,
                                    Url = a.Url,
                                    Nombre = a.Nombre,
                                    StorageProvider = a.StorageProvider.HasValue ? (int)a.StorageProvider.Value : null,
                                    StorageKey = a.StorageKey,
                                    ContentType = a.ContentType,
                                    SizeBytes = a.SizeBytes
                                })
                                .ToList()
                        }).FirstOrDefault(),
                    FeedbackHistorial = e.Feedbacks
                        .OrderByDescending(f => f.FechaCorreccionUtc)
                        .Select(f => new FeedbackHistoryItemModel
                        {
                            FeedbackId = f.Id,
                            EsVigente = f.EsVigente,
                            Estado = f.Estado,
                            Nota = f.Nota,
                            Comentario = f.Comentario,
                            FechaCorreccionUtc = f.FechaCorreccionUtc
                        }).ToList()
                })
                .FirstOrDefaultAsync(ct);

            if (entrega == null)
                return ResponseApiService.Response(StatusCodes.Status200OK, new { entregada = false });

            return ResponseApiService.Response(StatusCodes.Status200OK, new { entregada = true, entrega });
        }
    }
}
