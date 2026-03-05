using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteAsistenciaByClase
{
    public class GetReporteAsistenciasByCursoQuery : IGetReporteAsistenciasByCursoQuery
    {
        private readonly IDataBaseService _db;
        public GetReporteAsistenciasByCursoQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int profesorUserId,
            bool isAdmin,
            DateOnly from,
            DateOnly to,
            int pageNumber,
            int pageSize,
            string? search,
            CancellationToken ct)
        {
            if (cursoId <= 0) return ResponseApiService.Response(400, "CursoId inválido");
            if (to < from) return ResponseApiService.Response(400, "Rango de fechas inválido");

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var cursoExists = await _db.Cursos.AsNoTracking().AnyAsync(x => x.Id == cursoId, ct);
            if (!cursoExists) return ResponseApiService.Response(404, "Curso no encontrado");

            // Seguridad: prof asignado al curso
            if (!isAdmin)
            {
                var profAsignado = await _db.CursoProfesores.AsNoTracking()
                    .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);

                if (!profAsignado)
                    return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Profesor no asignado a este curso");
            }

            //  Base alumnos (con search)
            var alumnosQ = _db.Matriculas.AsNoTracking()
                .Where(m => m.CursoId == cursoId)
                .Select(m => new
                {
                    m.AlumnoId,
                    Nombre = m.Alumno.Usuario.Nombre,
                    Apellido = m.Alumno.Usuario.Apellido,
                    Dni = m.Alumno.Usuario.Dni
                });

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                alumnosQ = alumnosQ.Where(x =>
                    (x.Apellido ?? "").ToLower().Contains(s) ||
                    (x.Nombre ?? "").ToLower().Contains(s) ||
                    x.Dni.ToString().Contains(s));
            }

            var total = await alumnosQ.CountAsync(ct);


            var alumnosPage = await alumnosQ
                .OrderBy(x => x.Apellido).ThenBy(x => x.Nombre)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var alumnoIds = alumnosPage.Select(x => x.AlumnoId).ToList();


            var claseIdsQ = _db.Clases.AsNoTracking()
                .Where(c => c.CursoId == cursoId && c.Fecha >= from && c.Fecha <= to)
                .Select(c => c.Id);

            var stats = await (
                from a in _db.Asistencias.AsNoTracking()
                join claseId in claseIdsQ on a.ClaseId equals claseId
                where alumnoIds.Contains(a.AlumnoId)
                group a by a.AlumnoId into g
                select new
                {
                    AlumnoId = g.Key,
                    Presentes = g.Count(x => x.Estado == EstadoAsistencia.Presente),
                    Ausentes = g.Count(x => x.Estado == EstadoAsistencia.Ausente),
                    TotalRegistradas = g.Count()
                }
            ).ToListAsync(ct);

            var statsByAlumno = stats.ToDictionary(x => x.AlumnoId);


            var items = alumnosPage.Select(a =>
            {
                statsByAlumno.TryGetValue(a.AlumnoId, out var st);

                var presentes = st?.Presentes ?? 0;
                var ausentes = st?.Ausentes ?? 0;
                var totalReg = st?.TotalRegistradas ?? 0;

                return new AsistenciaReporteItemModel
                {
                    AlumnoId = a.AlumnoId,
                    Nombre = a.Nombre ?? "",
                    Apellido = a.Apellido ?? "",
                    Presentes = presentes,
                    Ausentes = ausentes,
                    TotalRegistradas = totalReg,
                    PorcentajePresencia = totalReg == 0 ? 0 : Math.Round((decimal)presentes * 100m / totalReg, 2)
                };
            }).ToList();

            var totalAlumnos = await _db.Matriculas.AsNoTracking().CountAsync(m => m.CursoId == cursoId, ct);

            // Total de clases "con asistencia cargada" en el rango
            var totalClasesConAsistencia = await _db.Asistencias.AsNoTracking()
                .Join(_db.Clases.AsNoTracking()
                        .Where(c => c.CursoId == cursoId && c.Fecha >= from && c.Fecha <= to),
                    a => a.ClaseId,
                    c => c.Id,
                    (a, c) => a.ClaseId)
                .Distinct()
                .CountAsync(ct);

            return ResponseApiService.Response(200, new
            {
                cursoId,
                from = from.ToString("yyyy-MM-dd"),
                to = to.ToString("yyyy-MM-dd"),
                pageNumber,
                pageSize,
                total,
                totalAlumnos,
                totalClasesConAsistencia,
                items
            });
        }

    }
}
