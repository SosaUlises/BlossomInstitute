using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Calificacion;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Entidades.Entrega;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteStudentSummaryByCursoAndTerm
{
    public class GetReporteStudentSummaryByCursoAndTermQuery : IGetReporteStudentSummaryByCursoAndTermQuery
    {
        private readonly IDataBaseService _db;

        public GetReporteStudentSummaryByCursoAndTermQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int alumnoId,
            int year,
            int term,
            int userId,
            bool isAdmin,
            CancellationToken ct)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "CursoId inválido");

            if (alumnoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "AlumnoId inválido");

            if (year < 2000 || year > 2100)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Year inválido");

            if (term < 1 || term > 3)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Term inválido. Valores permitidos: 1, 2 o 3.");

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

            var alumno = await _db.Alumnos
                .AsNoTracking()
                .Where(x => x.Id == alumnoId)
                .Select(x => new
                {
                    x.Id,
                    Nombre = x.Usuario.Nombre,
                    Apellido = x.Usuario.Apellido,
                    Dni = x.Usuario.Dni,
                    Email = x.Usuario.Email
                })
                .FirstOrDefaultAsync(ct);

            if (alumno == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Alumno no encontrado");

            var matriculado = await _db.Matriculas
                .AsNoTracking()
                .AnyAsync(x => x.CursoId == cursoId && x.AlumnoId == alumnoId, ct);

            if (!matriculado)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Alumno no matriculado en el curso");

            var (from, to) = GetTermRange(year, term);
            var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var toUtcExclusive = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            // =========================
            // ATTENDANCE
            // =========================
            var clasesTerm = await _db.Clases
                .AsNoTracking()
                .Where(x =>
                    x.CursoId == cursoId &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha >= from &&
                    x.Fecha <= to)
                .Select(x => x.Id)
                .ToListAsync(ct);

            var clasesTotales = clasesTerm.Count;

            var asistenciasAlumno = await _db.Asistencias
                .AsNoTracking()
                .Where(x =>
                    x.AlumnoId == alumnoId &&
                    clasesTerm.Contains(x.ClaseId))
                .ToListAsync(ct);

            var presentes = asistenciasAlumno.Count(x => x.Estado == EstadoAsistencia.Presente);
            var ausentes = asistenciasAlumno.Count(x => x.Estado == EstadoAsistencia.Ausente);

            var attendance = new ReporteStudentSummaryAttendanceModel
            {
                ClasesTotales = clasesTotales,
                Presentes = presentes,
                Ausentes = ausentes,
                PorcentajeAsistencia = clasesTotales > 0
                    ? Math.Round((decimal)presentes * 100 / clasesTotales, 2)
                    : 0
            };

            // =========================
            //   HOMEWORK
            // =========================
            var tareasHomeworkTerm = await _db.Tareas
                .AsNoTracking()
                .Where(x =>
                    x.CursoId == cursoId &&
                    x.FechaEntregaUtc.HasValue &&
                    x.FechaEntregaUtc.Value >= fromUtc &&
                    x.FechaEntregaUtc.Value < toUtcExclusive)
                .Select(x => new
                {
                    x.Id,
                    x.Titulo
                })
                .ToListAsync(ct);

            var tareaIdsHomeworkTerm = tareasHomeworkTerm
                .Select(x => x.Id)
                .ToList();

            var entregasAlumnoHomework = await _db.Entregas
                .AsNoTracking()
                .Where(x =>
                    x.AlumnoId == alumnoId &&
                    tareaIdsHomeworkTerm.Contains(x.TareaId))
                .Select(x => new
                {
                    x.Id,
                    x.TareaId
                })
                .ToListAsync(ct);

            var entregaIdsAlumnoHomework = entregasAlumnoHomework
                .Select(x => x.Id)
                .ToList();

            var feedbacksVigentesHomework = await _db.EntregaFeedbacks
                .AsNoTracking()
                .Where(x =>
                    x.EsVigente &&
                    entregaIdsAlumnoHomework.Contains(x.EntregaId))
                .Select(x => new
                {
                    x.EntregaId,
                    x.Estado
                })
                .ToListAsync(ct);

            var homeworkCalificaciones = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    x.CursoId == cursoId &&
                    x.AlumnoId == alumnoId &&
                    !x.Archivado &&
                    x.Tipo == TipoCalificacion.Homework &&
                    x.TareaId.HasValue &&
                    tareaIdsHomeworkTerm.Contains(x.TareaId.Value))
                .Select(x => new
                {
                    TareaId = x.TareaId!.Value,
                    x.Nota
                })
                .ToListAsync(ct);

            var entregasByTareaId = entregasAlumnoHomework
                .GroupBy(x => x.TareaId)
                .ToDictionary(g => g.Key, g => g.First());

            var feedbackByEntregaId = feedbacksVigentesHomework
                .GroupBy(x => x.EntregaId)
                .ToDictionary(g => g.Key, g => g.First());

            var homeworkCalificacionByTareaId = homeworkCalificaciones
                .GroupBy(x => x.TareaId)
                .ToDictionary(g => g.Key, g => g.First());

            var homeworkTotal = tareaIdsHomeworkTerm.Count;
            var homeworkEntregadas = 0;
            var homeworkSinEntregar = 0;
            var homeworkPendientesCorreccion = 0;
            var homeworkRehacer = 0;
            var homeworkAprobadas = 0;
            var homeworkNotas = new List<decimal>();

            foreach (var tareaId in tareaIdsHomeworkTerm)
            {
                if (!entregasByTareaId.TryGetValue(tareaId, out var entrega))
                {
                    homeworkSinEntregar++;
                    continue;
                }

                homeworkEntregadas++;

                if (feedbackByEntregaId.TryGetValue(entrega.Id, out var feedback))
                {
                    if (feedback.Estado == EstadoCorreccion.Rehacer)
                        homeworkRehacer++;

                    if (homeworkCalificacionByTareaId.TryGetValue(tareaId, out var hwCalificacion))
                    {
                        homeworkAprobadas++;
                        homeworkNotas.Add(hwCalificacion.Nota);
                    }
                }
                else
                {
                    homeworkPendientesCorreccion++;
                }
            }

            var homework = new ReporteStudentSummaryHomeworkModel
            {
                HomeworkTotal = homeworkTotal,
                HomeworkEntregadas = homeworkEntregadas,
                HomeworkSinEntregar = homeworkSinEntregar,
                HomeworkPendientesCorreccion = homeworkPendientesCorreccion,
                HomeworkRehacer = homeworkRehacer,
                HomeworkAprobadas = homeworkAprobadas,
                HomeworkPromedio = homeworkNotas.Count > 0
                    ? Math.Round(homeworkNotas.Average(), 2)
                    : null
            };

            // =========================
            // MARKS (QUIZZES + TESTS)
            // =========================
            var marks = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    x.CursoId == cursoId &&
                    x.AlumnoId == alumnoId &&
                    !x.Archivado &&
                    x.Fecha >= from &&
                    x.Fecha <= to &&
                    (x.Tipo == TipoCalificacion.Quiz || x.Tipo == TipoCalificacion.Test))
                .ToListAsync(ct);

            var quizzes = marks.Where(x => x.Tipo == TipoCalificacion.Quiz).ToList();
            var tests = marks.Where(x => x.Tipo == TipoCalificacion.Test).ToList();

            var marksSummary = new ReporteStudentSummaryMarksModel
            {
                QuizCount = quizzes.Count,
                QuizPromedio = quizzes.Count > 0 ? Math.Round(quizzes.Average(x => x.Nota), 2) : null,
                TestCount = tests.Count,
                TestPromedio = tests.Count > 0 ? Math.Round(tests.Average(x => x.Nota), 2) : null,
                MarksCount = marks.Count,
                PromedioGeneral = marks.Count > 0 ? Math.Round(marks.Average(x => x.Nota), 2) : null
            };

            // =========================
            // SKILLS
            // =========================
            var markIds = marks.Select(x => x.Id).ToList();

            var skills = await _db.CalificacionDetalles
                .AsNoTracking()
                .Where(x => markIds.Contains(x.CalificacionId))
                .GroupBy(x => x.Skill)
                .Select(g => new ReporteStudentSummarySkillItemModel
                {
                    Skill = g.Key,
                    EvaluacionesCount = g.Count(),
                    TotalObtenido = g.Sum(x => x.PuntajeObtenido),
                    TotalMaximo = g.Sum(x => x.PuntajeMaximo),
                    Porcentaje = g.Sum(x => x.PuntajeMaximo) > 0
                        ? Math.Round(g.Sum(x => x.PuntajeObtenido) * 100 / g.Sum(x => x.PuntajeMaximo), 2)
                        : null
                })
                .OrderBy(x => x.Skill)
                .ToListAsync(ct);

            var response = new ReporteStudentSummaryByCursoAndTermResponseModel
            {
                CursoId = curso.Id,
                CursoNombre = curso.Nombre,
                AlumnoId = alumno.Id,
                AlumnoNombre = alumno.Nombre,
                AlumnoApellido = alumno.Apellido,
                AlumnoDni = alumno.Dni,
                AlumnoEmail = alumno.Email,
                Year = year,
                Term = term,
                From = from,
                To = to,
                Attendance = attendance,
                Homework = homework,
                Marks = marksSummary,
                Skills = skills
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
