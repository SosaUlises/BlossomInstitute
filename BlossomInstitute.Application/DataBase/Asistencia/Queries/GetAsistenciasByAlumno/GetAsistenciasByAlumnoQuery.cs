using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Asistencia.Queries.GetAsistenciasByAlumno
{
    public class GetAsistenciasByAlumnoQuery : IGetAsistenciasByAlumnoQuery
    {
        private readonly IDataBaseService _db;

        public GetAsistenciasByAlumnoQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int alumnoId, int cursoId, DateOnly? fechaDesde, DateOnly? fechaHasta, CancellationToken ct = default)
        {
            if (alumnoId <= 0 || cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Parametros invalidos");

            if (fechaDesde.HasValue && fechaHasta.HasValue && fechaDesde.Value > fechaHasta.Value)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "El rango de fechas es invalido (from > to)");

            var matriculado = await _db.Matriculas.AsNoTracking()
                .AnyAsync(m => m.CursoId == cursoId && m.AlumnoId == alumnoId, ct);

            if (!matriculado)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "El alumno no esta matriculado en el curso");

            var clases = _db.Clases.AsNoTracking()
                .Where(c => c.CursoId == cursoId);

            if (fechaDesde.HasValue) clases = clases.Where(c => c.Fecha >= fechaDesde.Value);
            if (fechaHasta.HasValue) clases = clases.Where(c => c.Fecha <= fechaHasta.Value);

            var data = await clases
                .OrderByDescending(c => c.Fecha)
                .Select(c => new AsistenciaAlumnoHistorialItemModel
                {
                    ClaseId = c.Id,
                    Fecha = c.Fecha.ToString("yyyy-MM-dd"),
                    EstadoClase = c.Estado,
                    Descripcion = c.Descripcion,
                    Estado = _db.Asistencias
                        .Where(a => a.ClaseId == c.Id && a.AlumnoId == alumnoId)
                        .Select(a => (EstadoAsistencia?)a.Estado)
                        .FirstOrDefault()
                })
                .ToListAsync(ct);

            return ResponseApiService.Response(StatusCodes.Status200OK, data);
        }
    }
}
