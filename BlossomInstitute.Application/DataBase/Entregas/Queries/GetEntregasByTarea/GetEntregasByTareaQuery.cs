using BlossomInstitute.Application.DataBase.Entregas.Queries.Models;
using BlossomInstitute.Common.Features;
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
            CancellationToken ct)
        {
            if (cursoId <= 0 || tareaId <= 0) return ResponseApiService.Response(400, "Parámetros inválidos");
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            // Profesor asignado al curso
            var profAsignado = await _db.CursoProfesores.AsNoTracking()
                .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);
            if (!profAsignado) return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Profesor no asignado a este curso");

            // Tarea existe y pertenece al curso
            var tareaOk = await _db.Tareas.AsNoTracking()
                .AnyAsync(t => t.Id == tareaId && t.CursoId == cursoId, ct);
            if (!tareaOk) return ResponseApiService.Response(StatusCodes.Status404NotFound, "Tarea no encontrada");

            var q = _db.Entregas.AsNoTracking()
                .Where(e => e.TareaId == tareaId)
                .Select(e => new
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
                q = q.Where(x =>
                    (x.AlumnoApellido ?? "").ToLower().Contains(s) ||
                    (x.AlumnoNombre ?? "").ToLower().Contains(s) ||
                    x.AlumnoDni.ToString().Contains(s));
            }

            var total = await q.CountAsync(ct);

            var data = await q
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
