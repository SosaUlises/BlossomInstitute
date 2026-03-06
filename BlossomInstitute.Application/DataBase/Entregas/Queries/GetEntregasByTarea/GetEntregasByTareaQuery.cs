using BlossomInstitute.Application.DataBase.Entregas.Queries.Models;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Entrega;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Entregas.Queries.GetEntregasByTarea
{
    public class GetEntregasByTareaQuery : IGetEntregasByTareaQuery
    {
        private readonly IDataBaseService _db;

        public GetEntregasByTareaQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(
        int cursoId,
        int tareaId,
        int profesorUserId,
        int pageNumber,
        int pageSize,
        string? search,
        int? estadoEntrega,
        int? estadoFeedback,
        bool? pendienteCorreccion,
        bool? soloConAdjuntos,
        CancellationToken ct)
        {
            if (cursoId <= 0 || tareaId <= 0) return ResponseApiService.Response(400, "Parámetros inválidos");
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var profAsignado = await _db.CursoProfesores.AsNoTracking()
                .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);
            if (!profAsignado) return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Profesor no asignado a este curso");

            var tareaOk = await _db.Tareas.AsNoTracking()
                .AnyAsync(t => t.Id == tareaId && t.CursoId == cursoId, ct);
            if (!tareaOk) return ResponseApiService.Response(StatusCodes.Status404NotFound, "Tarea no encontrada");

            var q = _db.Entregas.AsNoTracking()
                .Where(e => e.TareaId == tareaId);

            // Filtro por estado de entrega
            if (estadoEntrega.HasValue)
            {
                var st = (EstadoEntrega)estadoEntrega.Value;
                q = q.Where(e => e.Estado == st);
            }

            // Filtro solo con adjuntos
            if (soloConAdjuntos == true)
                q = q.Where(e => e.Adjuntos.Any());

            // Pendiente de corrección:
            // true  => NO tiene feedback vigente
            // false => sí tiene feedback vigente
            if (pendienteCorreccion.HasValue)
            {
                if (pendienteCorreccion.Value)
                    q = q.Where(e => !e.Feedbacks.Any(f => f.EsVigente));
                else
                    q = q.Where(e => e.Feedbacks.Any(f => f.EsVigente));
            }

            // Filtro por estado del feedback vigente
            if (estadoFeedback.HasValue)
            {
                var sf = (EstadoCorreccion)estadoFeedback.Value;
                q = q.Where(e => e.Feedbacks.Any(f => f.EsVigente && f.Estado == sf));
            }

            // Proyección 
            var projected = q.Select(e => new
            {
                e.Id,
                e.AlumnoId,
                AlumnoNombre = e.Alumno.Usuario.Nombre,
                AlumnoApellido = e.Alumno.Usuario.Apellido,
                AlumnoDni = e.Alumno.Usuario.Dni,
                e.FechaEntregaUtc,
                e.Estado,
                TieneAdjuntos = e.Adjuntos.Any(),
                FeedbackVigente = e.Feedbacks
                    .Where(f => f.EsVigente)
                    .Select(f => new FeedbackVigenteModel
                    {
                        FeedbackId = f.Id,
                        Estado = (int)f.Estado,
                        Nota = f.Nota,
                        FechaCorreccionUtc = f.FechaCorreccionUtc
                    })
                    .FirstOrDefault()
            });

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                projected = projected.Where(x =>
                    (x.AlumnoApellido ?? "").ToLower().Contains(s) ||
                    (x.AlumnoNombre ?? "").ToLower().Contains(s) ||
                    x.AlumnoDni.ToString().Contains(s));
            }

            var total = await projected.CountAsync(ct);

            var data = await projected
                .OrderBy(x => x.AlumnoApellido).ThenBy(x => x.AlumnoNombre)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new EntregaListItemModel
                {
                    EntregaId = x.Id,
                    AlumnoId = x.AlumnoId,
                    AlumnoNombre = x.AlumnoNombre ?? "",
                    AlumnoApellido = x.AlumnoApellido ?? "",
                    AlumnoDni = x.AlumnoDni,
                    FechaEntregaUtc = x.FechaEntregaUtc,
                    EstadoEntrega = (int)x.Estado,
                    TieneAdjuntos = x.TieneAdjuntos,
                    FeedbackVigente = x.FeedbackVigente
                })
                .ToListAsync(ct);

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
