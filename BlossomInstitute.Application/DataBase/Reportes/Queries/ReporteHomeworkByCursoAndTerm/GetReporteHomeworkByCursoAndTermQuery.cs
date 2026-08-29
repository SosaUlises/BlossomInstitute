using BlossomInstitute.Application.Common.Academico;
using BlossomInstitute.Application.DataBase.Reportes.Shared;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Calificacion;
using BlossomInstitute.Domain.Entidades.Entrega;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteHomeworkByCursoAndTerm
{
    public class GetReporteHomeworkByCursoAndTermQuery : IGetReporteHomeworkByCursoAndTermQuery
    {
        private readonly IDataBaseService _db;

        public GetReporteHomeworkByCursoAndTermQuery(IDataBaseService db)
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
            var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var toUtcExclusive = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

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
                    x.Titulo,
                    x.FechaEntregaUtc
                })
                .ToListAsync(ct);

            var tareaIdsTerm = tareasHomeworkTerm
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

            var entregasPage = await _db.Entregas
                .AsNoTracking()
                .Where(x =>
                    alumnoIdsPage.Contains(x.AlumnoId) &&
                    tareaIdsTerm.Contains(x.TareaId))
                .Select(x => new
                {
                    x.Id,
                    x.TareaId,
                    x.AlumnoId
                })
                .ToListAsync(ct);

            var entregaIdsPage = entregasPage
                .Select(x => x.Id)
                .ToList();

            var feedbacksVigentesPage = await _db.EntregaFeedbacks
                .AsNoTracking()
                .Where(x =>
                    x.EsVigente &&
                    entregaIdsPage.Contains(x.EntregaId))
                .Select(x => new
                {
                    x.EntregaId,
                    x.Estado
                })
                .ToListAsync(ct);

            var homeworkCalificacionesPage = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    x.CursoId == cursoId &&
                    alumnoIdsPage.Contains(x.AlumnoId) &&
                    tareaIdsTerm.Contains(x.TareaId ?? 0) &&
                    !x.Archivado &&
                    x.Tipo == TipoCalificacion.Homework)
                .Select(x => new
                {
                    x.AlumnoId,
                    x.TareaId,
                    x.Nota
                })
                .ToListAsync(ct);

            var entregasByAlumnoTarea = entregasPage
                .GroupBy(x => new { x.AlumnoId, x.TareaId })
                .ToDictionary(g => (g.Key.AlumnoId, g.Key.TareaId), g => g.First());

            var feedbackByEntregaId = feedbacksVigentesPage
                .GroupBy(x => x.EntregaId)
                .ToDictionary(g => g.Key, g => g.First());

            var calificacionByAlumnoTarea = homeworkCalificacionesPage
                .GroupBy(x => new { x.AlumnoId, TareaId = x.TareaId ?? 0 })
                .ToDictionary(g => (g.Key.AlumnoId, g.Key.TareaId), g => g.First());

            var items = new List<ReporteHomeworkByCursoAndTermItemModel>();

            foreach (var alumno in alumnosPage)
            {
                var homeworkTotal = tareaIdsTerm.Count;
                var entregadas = 0;
                var sinEntregar = 0;
                var pendientesCorreccion = 0;
                var rehacer = 0;
                var aprobadas = 0;
                var notasHomework = new List<decimal>();

                foreach (var tareaId in tareaIdsTerm)
                {
                    var key = (alumno.AlumnoId, tareaId);

                    if (!entregasByAlumnoTarea.TryGetValue(key, out var entrega))
                    {
                        sinEntregar++;
                        continue;
                    }

                    entregadas++;

                    if (feedbackByEntregaId.TryGetValue(entrega.Id, out var feedback))
                    {
                        if (feedback.Estado == EstadoCorreccion.Rehacer)
                            rehacer++;

                        if (calificacionByAlumnoTarea.TryGetValue(key, out var calificacionHomework))
                        {
                            aprobadas++;
                            notasHomework.Add(calificacionHomework.Nota);
                        }
                    }
                    else
                    {
                        pendientesCorreccion++;
                    }
                }

                items.Add(new ReporteHomeworkByCursoAndTermItemModel
                {
                    AlumnoId = alumno.AlumnoId,
                    AlumnoNombre = alumno.Nombre,
                    AlumnoApellido = alumno.Apellido,
                    AlumnoDni = alumno.Dni,
                    AlumnoEmail = alumno.Email,
                    HomeworkTotal = homeworkTotal,
                    HomeworkEntregadas = entregadas,
                    HomeworkSinEntregar = sinEntregar,
                    HomeworkPendientesCorreccion = pendientesCorreccion,
                    HomeworkRehacer = rehacer,
                    HomeworkAprobadas = aprobadas,
                    HomeworkPromedio = notasHomework.Count > 0 ? Math.Round(notasHomework.Average(), 2) : null
                });
            }

            var allAlumnoIdsCurso = await _db.Matriculas
                .AsNoTracking()
                .Where(x => x.CursoId == cursoId)
                .Select(x => x.AlumnoId)
                .Distinct()
                .ToListAsync(ct);

            var entregasCurso = await _db.Entregas
                .AsNoTracking()
                .Where(x =>
                    x.Tarea.CursoId == cursoId &&
                    tareaIdsTerm.Contains(x.TareaId))
                .Select(x => new
                {
                    x.Id,
                    x.TareaId,
                    x.AlumnoId
                })
                .ToListAsync(ct);

            var entregaIdsCurso = entregasCurso.Select(x => x.Id).ToList();

            var feedbacksVigentesCurso = await _db.EntregaFeedbacks
                .AsNoTracking()
                .Where(x =>
                    x.EsVigente &&
                    entregaIdsCurso.Contains(x.EntregaId))
                .Select(x => new
                {
                    x.EntregaId,
                    x.Estado
                })
                .ToListAsync(ct);

            var calificacionesHomeworkCurso = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    x.CursoId == cursoId &&
                    !x.Archivado &&
                    x.Tipo == TipoCalificacion.Homework &&
                    x.TareaId.HasValue &&
                    tareaIdsTerm.Contains(x.TareaId.Value))
                .Select(x => x.Nota)
                .ToListAsync(ct);

            var totalHomework = tareaIdsTerm.Count * allAlumnoIdsCurso.Count;
            var totalEntregas = entregasCurso
                .Select(x => new { x.AlumnoId, x.TareaId })
                .Distinct()
                .Count();

            var totalSinEntregar = totalHomework - totalEntregas;

            var feedbackByEntregaIdCurso = feedbacksVigentesCurso
                .GroupBy(x => x.EntregaId)
                .ToDictionary(g => g.Key, g => g.First());

            var totalPendientesCorreccion = entregasCurso.Count(x => !feedbackByEntregaIdCurso.ContainsKey(x.Id));
            var totalRehacer = feedbacksVigentesCurso.Count(x => x.Estado == EstadoCorreccion.Rehacer);
            var totalAprobadas = calificacionesHomeworkCurso.Count;

            var resumen = new ReporteHomeworkByCursoAndTermResumenModel
            {
                CursoId = curso.Id,
                CursoNombre = curso.Nombre,
                Year = year,
                Term = term,
                From = from,
                To = to,
                TotalAlumnos = allAlumnoIdsCurso.Count,
                TotalHomework = totalHomework,
                TotalEntregas = totalEntregas,
                TotalSinEntregar = totalSinEntregar,
                TotalPendientesCorreccion = totalPendientesCorreccion,
                TotalRehacer = totalRehacer,
                TotalAprobadas = totalAprobadas,
                PromedioHomeworkCurso = calificacionesHomeworkCurso.Count > 0
                    ? Math.Round(calificacionesHomeworkCurso.Average(), 2)
                    : null
            };

            var response = new ReporteHomeworkByCursoAndTermResponseModel
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
