using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Entrega;
using BlossomInstitute.Domain.Entidades.Tarea;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Tarea.Queries.GetTareasAlumno
{
    public class GetTareaAlumnoByIdQuery : IGetTareaAlumnoByIdQuery
    {
        private readonly IDataBaseService _db;

        public GetTareaAlumnoByIdQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int cursoId, int tareaId, int alumnoUserId, CancellationToken ct = default)
        {
            if (cursoId <= 0 || tareaId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Parámetros inválidos");

            if (alumnoUserId <= 0)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "No autenticado");

            var matriculado = await _db.Matriculas.AsNoTracking()
                .AnyAsync(m => m.CursoId == cursoId && m.AlumnoId == alumnoUserId, ct);

            if (!matriculado)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No estás matriculado en este curso");

            var nowUtc = DateTime.UtcNow;

            var tarea = await _db.Tareas
                .AsNoTracking()
                .Where(t => t.Id == tareaId && t.CursoId == cursoId && t.Estado == EstadoTarea.Publicada)
                .Select(t => new
                {
                    tareaId = t.Id,
                    t.CursoId,
                    profesorNombre = t.Profesor.Usuario.Nombre,
                    profesorApellido = t.Profesor.Usuario.Apellido,
                    profesorAvatarUrl = t.Profesor.Usuario.AvatarUrl,
                    t.Titulo,
                    descripcion = t.Consigna,
                    consigna = t.Consigna,
                    t.FechaEntregaUtc,
                    recursos = t.Recursos
                        .OrderBy(r => r.Id)
                        .Select(r => new
                        {
                            r.Id,
                            tipo = (int)r.Tipo,
                            r.Url,
                            r.Nombre,
                            r.ContentType,
                            r.SizeBytes
                        })
                        .ToList(),
                    vencida = t.FechaEntregaUtc.HasValue && t.FechaEntregaUtc.Value < nowUtc,
                    miEntrega = _db.Entregas
                        .Where(e => e.TareaId == t.Id && e.AlumnoId == alumnoUserId)
                        .Select(e => new
                        {
                            entregaId = e.Id,
                            contenido = e.Texto,
                            archivoUrl = e.Adjuntos
                                .OrderBy(a => a.Id)
                                .Select(a => a.Url)
                                .FirstOrDefault(),
                            e.FechaEntregaUtc,
                            estado = (int)e.Estado,
                            feedbackVigente = e.Feedbacks
                                .Where(f => f.EsVigente)
                                .OrderByDescending(f => f.FechaCorreccionUtc)
                                .Select(f => new
                                {
                                    feedbackId = f.Id,
                                    profesorNombre = f.Entrega.Tarea.Profesor.Usuario.Nombre,
                                    profesorApellido = f.Entrega.Tarea.Profesor.Usuario.Apellido,
                                    profesorAvatarUrl = f.Entrega.Tarea.Profesor.Usuario.AvatarUrl,
                                    estado = (int)f.Estado,
                                    f.Nota,
                                    f.Comentario,
                                    f.FechaCorreccionUtc,
                                    adjuntos = f.Adjuntos
                                        .OrderBy(a => a.CreatedAtUtc)
                                        .Select(a => new
                                        {
                                            a.Id,
                                            tipo = (int)a.Tipo,
                                            a.Url,
                                            a.Nombre,
                                            storageProvider = a.StorageProvider.HasValue ? (int)a.StorageProvider.Value : (int?)null,
                                            a.StorageKey,
                                            a.ContentType,
                                            a.SizeBytes
                                        })
                                        .ToList()
                                })
                                .FirstOrDefault()
                        })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync(ct);

            if (tarea == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Tarea no encontrada");

            return ResponseApiService.Response(StatusCodes.Status200OK, tarea);
        }
    }
}
