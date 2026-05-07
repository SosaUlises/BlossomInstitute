using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Entrega;
using BlossomInstitute.Domain.Entidades.Tarea;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Tarea.Queries.GetTareasAlumno
{
    public class GetTareasAlumnoByCursoQuery : IGetTareasAlumnoByCursoQuery
    {
        private readonly IDataBaseService _db;

        public GetTareasAlumnoByCursoQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int cursoId, int alumnoUserId, int pageNumber, int pageSize, CancellationToken ct = default)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "CursoId invÃ¡lido");

            if (alumnoUserId <= 0)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "No autenticado");

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var matriculado = await _db.Matriculas.AsNoTracking()
                .AnyAsync(m => m.CursoId == cursoId && m.AlumnoId == alumnoUserId, ct);

            if (!matriculado)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No estÃ¡s matriculado en este curso");

            var nowUtc = DateTime.UtcNow;

            var query = _db.Tareas
                .AsNoTracking()
                .Where(t => t.CursoId == cursoId && t.Estado == EstadoTarea.Publicada);

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(t => t.CreatedAtUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    tareaId = t.Id,
                    t.CursoId,
                    t.ProfesorId,
                    profesorNombre = t.Profesor.Usuario.Nombre,
                    profesorApellido = t.Profesor.Usuario.Apellido,
                    t.Titulo,
                    descripcion = t.Consigna,
                    t.FechaEntregaUtc,
                    t.CreatedAtUtc,
                    esAnuncio = !t.FechaEntregaUtc.HasValue,
                    recursosCount = t.Recursos.Count,
                    tieneRecursos = t.Recursos.Count > 0,
                    vencida = t.FechaEntregaUtc.HasValue && t.FechaEntregaUtc.Value < nowUtc,
                    tieneEntrega = _db.Entregas.Any(e => e.TareaId == t.Id && e.AlumnoId == alumnoUserId),
                    estadoEntrega = _db.Entregas
                        .Where(e => e.TareaId == t.Id && e.AlumnoId == alumnoUserId)
                        .Select(e => (int?)e.Estado)
                        .FirstOrDefault(),
                    feedbackPendienteAccion = _db.EntregaFeedbacks
                        .Any(f =>
                            f.EsVigente &&
                            f.Estado == EstadoCorreccion.Rehacer &&
                            f.Entrega.TareaId == t.Id &&
                            f.Entrega.AlumnoId == alumnoUserId)
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
