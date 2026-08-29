using BlossomInstitute.Application.Common.Academico;
using BlossomInstitute.Application.DataBase.Reportes.Shared;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteAttendanceByCursoAndTerm
{
    public class GetReporteAttendanceByCursoAndTermQuery : IGetReporteAttendanceByCursoAndTermQuery
    {
        private readonly IDataBaseService _db;

        public GetReporteAttendanceByCursoAndTermQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int year,
            int term,
            int userId,
            bool isAdmin,
            int pageNumber,
            int pageSize,
            string? search,
            CancellationToken ct)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "CursoId inválido");

            if (year < 2000 || year > 2100)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Year inválido");

            if (term < 1 || term > 3)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Term inválido. Valores permitidos: 1, 2 o 3.");

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var curso = await _db.Cursos
                .AsNoTracking()
                .Where(x => x.Id == cursoId)
                .Select(x => new
                {
                    x.Id,
                    x.Nombre
                })
                .FirstOrDefaultAsync(ct);

            if (curso == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Curso no encontrado");

            if (!isAdmin)
            {
                var profesorAsignado = await _db.CursoProfesores
                    .AsNoTracking()
                    .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == userId, ct);

                if (!profesorAsignado)
                    return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No autorizado");
            }

            var academicPeriod = PeriodoAcademicoHelper.ObtenerTrimestre(year, term);
            var from = academicPeriod.Desde;
            var to = academicPeriod.Hasta;

            var clasesTerm = await _db.Clases
                .AsNoTracking()
                .Where(x =>
                    x.CursoId == cursoId &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha >= from &&
                    x.Fecha <= to)
                .Select(x => new
                {
                    x.Id,
                    x.Fecha
                })
                .ToListAsync(ct);

            var claseIdsTerm = clasesTerm
                .Select(x => x.Id)
                .ToList();

            var alumnosBaseQuery = _db.Matriculas
                .AsNoTracking()
                .Where(x => x.CursoId == cursoId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                alumnosBaseQuery = alumnosBaseQuery.Where(x =>
                    x.Alumno.Usuario.Nombre.Contains(search) ||
                    x.Alumno.Usuario.Apellido.Contains(search) ||
                    x.Alumno.Usuario.Email!.Contains(search) ||
                    x.Alumno.Usuario.Dni.ToString().Contains(search));
            }

            var total = await alumnosBaseQuery.CountAsync(ct);

            var alumnosPage = await alumnosBaseQuery
                .OrderBy(x => x.Alumno.Usuario.Apellido)
                .ThenBy(x => x.Alumno.Usuario.Nombre)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.AlumnoId,
                    Nombre = x.Alumno.Usuario.Nombre,
                    Apellido = x.Alumno.Usuario.Apellido,
                    Dni = x.Alumno.Usuario.Dni,
                    Email = x.Alumno.Usuario.Email
                })
                .ToListAsync(ct);

            var alumnoIdsPage = alumnosPage
                .Select(x => x.AlumnoId)
                .ToList();

            var asistenciasPage = await _db.Asistencias
                .AsNoTracking()
                .Where(x =>
                    alumnoIdsPage.Contains(x.AlumnoId) &&
                    claseIdsTerm.Contains(x.ClaseId))
                .GroupBy(x => x.AlumnoId)
                .Select(g => new
                {
                    AlumnoId = g.Key,
                    Presentes = g.Count(x => x.Estado == EstadoAsistencia.Presente),
                    Ausentes = g.Count(x => x.Estado == EstadoAsistencia.Ausente)
                })
                .ToListAsync(ct);

            var asistenciasPageDict = asistenciasPage.ToDictionary(x => x.AlumnoId);

            var clasesTotalesTerm = claseIdsTerm.Count;

            var items = alumnosPage
                .Select(a =>
                {
                    asistenciasPageDict.TryGetValue(a.AlumnoId, out var asistencia);

                    var presentes = asistencia?.Presentes ?? 0;
                    var ausentes = asistencia?.Ausentes ?? 0;

                    var porcentaje = clasesTotalesTerm > 0
                        ? Math.Round((decimal)presentes * 100 / clasesTotalesTerm, 2)
                        : 0;

                    return new ReporteAttendanceByCursoAndTermItemModel
                    {
                        AlumnoId = a.AlumnoId,
                        AlumnoNombre = a.Nombre,
                        AlumnoApellido = a.Apellido,
                        AlumnoDni = a.Dni,
                        AlumnoEmail = a.Email,
                        ClasesTotales = clasesTotalesTerm,
                        Presentes = presentes,
                        Ausentes = ausentes,
                        PorcentajeAsistencia = porcentaje
                    };
                })
                .ToList();

            var allAlumnoIdsCurso = await _db.Matriculas
                .AsNoTracking()
                .Where(x => x.CursoId == cursoId)
                .Select(x => x.AlumnoId)
                .Distinct()
                .ToListAsync(ct);

            var asistenciasCurso = await _db.Asistencias
                .AsNoTracking()
                .Where(x =>
                    allAlumnoIdsCurso.Contains(x.AlumnoId) &&
                    claseIdsTerm.Contains(x.ClaseId))
                .ToListAsync(ct);

            var totalAlumnosCurso = allAlumnoIdsCurso.Count;
            var totalClases = clasesTotalesTerm;
            var totalPresentes = asistenciasCurso.Count(x => x.Estado == EstadoAsistencia.Presente);
            var totalAusentes = asistenciasCurso.Count(x => x.Estado == EstadoAsistencia.Ausente);

            var totalEsperadoRegistros = totalAlumnosCurso * totalClases;

            var porcentajeAsistenciaCurso = totalEsperadoRegistros > 0
                ? Math.Round((decimal)totalPresentes * 100 / totalEsperadoRegistros, 2)
                : (decimal?)null;

            var resumen = new ReporteAttendanceByCursoAndTermResumenModel
            {
                CursoId = curso.Id,
                CursoNombre = curso.Nombre,
                Year = year,
                Term = term,
                From = from,
                To = to,
                TotalAlumnos = totalAlumnosCurso,
                TotalClases = totalClases,
                TotalPresentes = totalPresentes,
                TotalAusentes = totalAusentes,
                PorcentajeAsistenciaCurso = porcentajeAsistenciaCurso
            };

            var response = new ReporteAttendanceByCursoAndTermResponseModel
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Total = total,
                Period = ReportAcademicPeriodModel.FromQuarter(academicPeriod),
                Resumen = resumen,
                Items = items
            };

            return ResponseApiService.Response(StatusCodes.Status200OK, response);
        }
    }
}
