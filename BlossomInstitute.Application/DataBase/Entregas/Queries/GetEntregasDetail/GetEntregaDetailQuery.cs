using BlossomInstitute.Application.DataBase.Entregas.Queries.Models;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Entregas.Queries.GetEntregasDetail
{
    public class GetEntregaDetailQuery : IGetEntregaDetailQuery
    {
        private readonly IDataBaseService _db;

        public GetEntregaDetailQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int cursoId, int tareaId, int alumnoId, int profesorUserId, CancellationToken ct)
        {
            if (cursoId <= 0 || tareaId <= 0 || alumnoId <= 0) return ResponseApiService.Response(400, "Parámetros inválidos");

            // Profesor asignado
            var profAsignado = await _db.CursoProfesores.AsNoTracking()
                .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);
            if (!profAsignado) return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Profesor no asignado a este curso");

            // Tarea pertenece al curso
            var tareaOk = await _db.Tareas.AsNoTracking()
                .AnyAsync(t => t.Id == tareaId && t.CursoId == cursoId, ct);
            if (!tareaOk) return ResponseApiService.Response(StatusCodes.Status404NotFound, "Tarea no encontrada");

            // Entrega
            var entrega = await _db.Entregas.AsNoTracking()
                .Where(e => e.TareaId == tareaId && e.AlumnoId == alumnoId)
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
                            Nota = f.Nota,
                            FechaCorreccionUtc = f.FechaCorreccionUtc
                        }).FirstOrDefault(),
                    FeedbackHistorial = e.Feedbacks
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
                        }).ToList()
                })
                .FirstOrDefaultAsync(ct);

            if (entrega == null) return ResponseApiService.Response(StatusCodes.Status404NotFound, "Entrega no encontrada");

            return ResponseApiService.Response(StatusCodes.Status200OK, entrega);
        }
    }
}
