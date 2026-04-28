using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Calificacion;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteMarksByCursoAndTerm
{
    public class GetReporteMarksByCursoAndTermQuery : IGetReporteMarksByCursoAndTermQuery
    {
        private readonly IDataBaseService _db;

        public GetReporteMarksByCursoAndTermQuery(IDataBaseService db)
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

            var (from, to) = GetTermRange(year, term);

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

            var marksPage = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    x.CursoId == cursoId &&
                    alumnoIdsPage.Contains(x.AlumnoId) &&
                    !x.Archivado &&
                    x.Fecha >= from &&
                    x.Fecha <= to &&
                    (x.Tipo == TipoCalificacion.Quiz || x.Tipo == TipoCalificacion.Test))
                .GroupBy(x => x.AlumnoId)
                .Select(g => new
                {
                    AlumnoId = g.Key,
                    QuizCount = g.Count(x => x.Tipo == TipoCalificacion.Quiz),
                    QuizPromedio = g.Where(x => x.Tipo == TipoCalificacion.Quiz)
                        .Select(x => (decimal?)x.Nota)
                        .Average(),
                    TestCount = g.Count(x => x.Tipo == TipoCalificacion.Test),
                    TestPromedio = g.Where(x => x.Tipo == TipoCalificacion.Test)
                        .Select(x => (decimal?)x.Nota)
                        .Average(),
                    MarksCount = g.Count(),
                    PromedioGeneral = g.Select(x => (decimal?)x.Nota).Average()
                })
                .ToListAsync(ct);

            var marksPageDict = marksPage.ToDictionary(x => x.AlumnoId);

            var items = alumnosPage
                .Select(a =>
                {
                    marksPageDict.TryGetValue(a.AlumnoId, out var marks);

                    return new ReporteMarksByCursoAndTermItemModel
                    {
                        AlumnoId = a.AlumnoId,
                        AlumnoNombre = a.Nombre,
                        AlumnoApellido = a.Apellido,
                        AlumnoDni = a.Dni,
                        AlumnoEmail = a.Email,
                        QuizCount = marks?.QuizCount ?? 0,
                        QuizPromedio = marks?.QuizPromedio.HasValue == true ? Math.Round(marks.QuizPromedio.Value, 2) : null,
                        TestCount = marks?.TestCount ?? 0,
                        TestPromedio = marks?.TestPromedio.HasValue == true ? Math.Round(marks.TestPromedio.Value, 2) : null,
                        MarksCount = marks?.MarksCount ?? 0,
                        PromedioGeneral = marks?.PromedioGeneral.HasValue == true ? Math.Round(marks.PromedioGeneral.Value, 2) : null
                    };
                })
                .ToList();

            var marksCurso = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    x.CursoId == cursoId &&
                    !x.Archivado &&
                    x.Fecha >= from &&
                    x.Fecha <= to &&
                    (x.Tipo == TipoCalificacion.Quiz || x.Tipo == TipoCalificacion.Test))
                .ToListAsync(ct);

            var totalAlumnosCurso = await _db.Matriculas
                .AsNoTracking()
                .Where(x => x.CursoId == cursoId)
                .Select(x => x.AlumnoId)
                .Distinct()
                .CountAsync(ct);

            var alumnosConNotas = marksCurso
                .Select(x => x.AlumnoId)
                .Distinct()
                .Count();

            var quizzes = marksCurso.Where(x => x.Tipo == TipoCalificacion.Quiz).ToList();
            var tests = marksCurso.Where(x => x.Tipo == TipoCalificacion.Test).ToList();

            var resumen = new ReporteMarksByCursoAndTermResumenModel
            {
                CursoId = curso.Id,
                CursoNombre = curso.Nombre,
                Year = year,
                Term = term,
                From = from,
                To = to,
                TotalAlumnos = totalAlumnosCurso,
                AlumnosConNotas = alumnosConNotas,
                TotalQuizzes = quizzes.Count,
                PromedioQuizzesCurso = quizzes.Count > 0 ? Math.Round(quizzes.Average(x => x.Nota), 2) : null,
                TotalTests = tests.Count,
                PromedioTestsCurso = tests.Count > 0 ? Math.Round(tests.Average(x => x.Nota), 2) : null,
                TotalMarks = marksCurso.Count,
                PromedioGeneralCurso = marksCurso.Count > 0 ? Math.Round(marksCurso.Average(x => x.Nota), 2) : null
            };

            var response = new ReporteMarksByCursoAndTermResponseModel
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Total = total,
                Resumen = resumen,
                Items = items
            };

            return ResponseApiService.Response(StatusCodes.Status200OK, response);
        }

        private static (DateOnly from, DateOnly to) GetTermRange(int year, int term)
        {
            return term switch
            {
                1 => (new DateOnly(year, 3, 1), new DateOnly(year, 5, 31)),
                2 => (new DateOnly(year, 6, 1), new DateOnly(year, 8, 31)),
                3 => (new DateOnly(year, 9, 1), new DateOnly(year, 11, 30)),
                _ => throw new ArgumentOutOfRangeException(nameof(term))
            };
        }
    }
}
