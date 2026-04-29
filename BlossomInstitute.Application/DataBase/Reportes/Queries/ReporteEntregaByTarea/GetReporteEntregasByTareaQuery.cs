using BlossomInstitute.Application.DataBase.Entregas.Queries.Models;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Entrega;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteEntregaByTarea
{
    public class GetReporteEntregasByTareaQuery : IGetReporteEntregasByTareaQuery
    {
        private readonly IDataBaseService _db;

        public GetReporteEntregasByTareaQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int tareaId,
            int profesorUserId,
            bool isAdmin,
            int pageNumber,
            int pageSize,
            string? search,
            EstadoEntregaReporte? estado,
            bool? pendienteCorreccion,
            CancellationToken ct)
        {
            if (cursoId <= 0 || tareaId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Parámetros inválidos");

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            if (!isAdmin)
            {
                var profAsignado = await _db.CursoProfesores.AsNoTracking()
                    .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);

                if (!profAsignado)
                    return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Profesor no asignado a este curso");
            }

            var tareaOk = await _db.Tareas.AsNoTracking()
                .AnyAsync(t => t.Id == tareaId && t.CursoId == cursoId, ct);

            if (!tareaOk)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Tarea no encontrada");

            var q =
                from m in _db.Matriculas.AsNoTracking()
                where m.CursoId == cursoId
                join e in _db.Entregas.AsNoTracking().Where(x => x.TareaId == tareaId)
                    on m.AlumnoId equals e.AlumnoId into entregas
                from e in entregas.DefaultIfEmpty()
                select new
                {
                    m.AlumnoId,
                    AlumnoNombre = m.Alumno.Usuario.Nombre,
                    AlumnoApellido = m.Alumno.Usuario.Apellido,
                    AlumnoDni = m.Alumno.Usuario.Dni,

                    EntregaId = (int?)e.Id,
                    FechaEntregaUtc = (DateTime?)e.FechaEntregaUtc,
                    EstadoEntrega = (EstadoEntrega?)e.Estado,
                    TieneAdjuntos = e != null && e.Adjuntos.Any(),

                    TieneFeedbackVigente = e != null && e.Feedbacks.Any(f => f.EsVigente),

                    FeedbackVigente = e == null
                        ? null
                        : e.Feedbacks
                            .Where(f => f.EsVigente)
                            .Select(f => new
                            {
                                f.Id,
                                Estado = (int)f.Estado,
                                f.FechaCorreccionUtc
                            })
                            .FirstOrDefault(),

                    CalificacionActiva = e == null
                        ? null
                        : _db.Calificaciones
                            .Where(c =>
                                c.CursoId == cursoId &&
                                c.AlumnoId == m.AlumnoId &&
                                c.TareaId == tareaId &&
                                c.EntregaId == e.Id &&
                                !c.Archivado)
                            .Select(c => new
                            {
                                c.Id,
                                c.Nota
                            })
                            .FirstOrDefault()
                };

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                q = q.Where(x =>
                    (x.AlumnoApellido ?? "").ToLower().Contains(s) ||
                    (x.AlumnoNombre ?? "").ToLower().Contains(s) ||
                    x.AlumnoDni.ToString().Contains(s));
            }

            if (pendienteCorreccion.HasValue)
            {
                if (pendienteCorreccion.Value)
                    q = q.Where(x => x.EntregaId != null && !x.TieneFeedbackVigente);
                else
                    q = q.Where(x => x.EntregaId != null && x.TieneFeedbackVigente);
            }

            if (estado.HasValue)
            {
                switch (estado.Value)
                {
                    case EstadoEntregaReporte.SinEntregar:
                        q = q.Where(x => x.EntregaId == null);
                        break;

                    case EstadoEntregaReporte.EntregadoEnTermino:
                        q = q.Where(x => x.EntregaId != null && x.EstadoEntrega == EstadoEntrega.EntregadaEnTermino);
                        break;

                    case EstadoEntregaReporte.EntregadoFueraDeTermino:
                        q = q.Where(x => x.EntregaId != null && x.EstadoEntrega == EstadoEntrega.FueraDeTermino);
                        break;
                }
            }

            var total = await q.CountAsync(ct);

            var items = await q
                .OrderBy(x => x.AlumnoApellido).ThenBy(x => x.AlumnoNombre)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new EntregaReporteListItemModel
                {
                    AlumnoId = x.AlumnoId,
                    AlumnoNombre = x.AlumnoNombre ?? "",
                    AlumnoApellido = x.AlumnoApellido ?? "",
                    AlumnoDni = x.AlumnoDni,

                    Estado = x.EntregaId == null
                        ? EstadoEntregaReporte.SinEntregar
                        : x.EstadoEntrega == EstadoEntrega.FueraDeTermino
                            ? EstadoEntregaReporte.EntregadoFueraDeTermino
                            : EstadoEntregaReporte.EntregadoEnTermino,

                    EntregaId = x.EntregaId,
                    FechaEntregaUtc = x.FechaEntregaUtc,
                    TieneAdjuntos = x.TieneAdjuntos,

                    FeedbackVigente = x.FeedbackVigente == null
                        ? null
                        : new FeedbackVigenteModel
                        {
                            FeedbackId = x.FeedbackVigente.Id,
                            Estado = x.FeedbackVigente.Estado,
                            Nota = x.CalificacionActiva == null ? null : x.CalificacionActiva.Nota,
                            FechaCorreccionUtc = x.FeedbackVigente.FechaCorreccionUtc
                        }
                })
                .ToListAsync(ct);

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
