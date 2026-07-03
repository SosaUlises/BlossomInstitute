using BlossomInstitute.Common.Features;
using BlossomInstitute.Application.Common.Academico;
using BlossomInstitute.Application.DataBase.Curso.Shared;
using BlossomInstitute.Domain.Entidades.Calificacion;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Entidades.Entrega;
using BlossomInstitute.Domain.Entidades.Tarea;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Dashboard.Queries.GetAdminDashboard
{
    public class GetAdminDashboardQuery : IGetAdminDashboardQuery
    {
        private readonly IDataBaseService _db;

        public GetAdminDashboardQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(
            int userId,
            bool isAdmin,
            CancellationToken ct)
        {
            if (userId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "UserId inválido");

            if (!isAdmin)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No autorizado");

            var nowUtc = DateTime.UtcNow;
            var nowLocal = DateTime.Now;
            var today = DateOnly.FromDateTime(nowLocal);
            var periodContext = PeriodoAcademicoHelper.ObtenerContexto(today);
            var currentQuarter = periodContext.TrimestreActual;
            var previousQuarter = periodContext.TrimestreAnterior;
            var currentDataTo = periodContext.Hasta;
            const int consecutiveAbsenceWindowDays = 21;
            var consecutiveAbsenceFrom = today.AddDays(-(consecutiveAbsenceWindowDays - 1));
            var periodFromUtc = currentQuarter.Desde.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var periodToUtcExclusive = currentDataTo.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var period = new DashboardPeriodModel
            {
                Type = "academic-quarter",
                Strategy = "academic-quarter",
                Label = periodContext.Etiqueta,
                MonthRangeLabel = currentQuarter.EtiquetaRangoMeses,
                From = periodContext.Desde,
                To = periodContext.Hasta,
                Year = periodContext.Anio,
                Month = currentQuarter.Desde.Month,
                Quarter = periodContext.NumeroTrimestre
            };

            var trendComparison = new DashboardTrendComparisonModel
            {
                Type = "previous-academic-quarter",
                Label = "trimestre anterior"
            };

            var consecutiveAbsencesWindow = new DashboardRollingWindowModel
            {
                Type = "rolling-days",
                Days = consecutiveAbsenceWindowDays,
                Label = $"últimos {consecutiveAbsenceWindowDays} días"
            };

            // -------------------------------------------------
            // OVERVIEW
            // -------------------------------------------------
            var studentsCount = await _db.Matriculas
                .AsNoTracking()
                .Select(x => x.AlumnoId)
                .Distinct()
                .CountAsync(ct);

            var teachersCount = await _db.CursoProfesores
                .AsNoTracking()
                .Select(x => x.ProfesorId)
                .Distinct()
                .CountAsync(ct);

            var activeCoursesCount = await _db.Cursos
                .AsNoTracking()
                .CountAsync(x => x.Estado == EstadoCurso.Activo, ct);

            var pendingAssignmentsCount = await _db.Tareas
                .AsNoTracking()
                .CountAsync(x =>
                    x.Estado == EstadoTarea.Publicada &&
                    x.FechaEntregaUtc.HasValue &&
                    x.FechaEntregaUtc.Value >= nowUtc,
                    ct);

            var overview = new DashboardOverviewModel
            {
                StudentsCount = studentsCount,
                TeachersCount = teachersCount,
                ActiveCoursesCount = activeCoursesCount,
                PendingAssignmentsCount = pendingAssignmentsCount
            };

            var courseTeacherNameRows = await _db.CursoProfesores
                .AsNoTracking()
                .Where(x => x.Curso.Estado == EstadoCurso.Activo)
                .Select(x => new
                {
                    x.CursoId,
                    ProfesorNombre = x.Profesor.Usuario.Nombre + " " + x.Profesor.Usuario.Apellido
                })
                .ToListAsync(ct);

            var courseTeacherNamesByCourse = courseTeacherNameRows
                .GroupBy(x => x.CursoId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ProfesorNombre)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct()
                        .OrderBy(x => x)
                        .ToList());

            List<string> GetCourseTeacherNames(int cursoId) =>
                courseTeacherNamesByCourse.TryGetValue(cursoId, out var names)
                    ? names
                    : new List<string>();

            // -------------------------------------------------
            // -------------------------------------------------
            // CURRENT PERIOD GENERAL AVERAGE (TODOS LOS TIPOS)
            // Strategy: current academic quarter. Keeping GeneralAverage populated
            // with this value for backwards compatibility with the frontend.
            // -------------------------------------------------
            var generalAverageRaw = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    !x.Archivado &&
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Fecha >= currentQuarter.Desde &&
                    x.Fecha <= currentDataTo &&
                    (
                        x.Tipo == TipoCalificacion.Homework ||
                        x.Tipo == TipoCalificacion.Quiz ||
                        x.Tipo == TipoCalificacion.Test ||
                        x.Tipo == TipoCalificacion.Participation ||
                        x.Tipo == TipoCalificacion.Behaviour
                    ))
                .Select(x => (decimal?)x.Nota)
                .AverageAsync(ct);

            decimal? generalAverage = generalAverageRaw.HasValue
                ? Math.Round(generalAverageRaw.Value, 2)
                : null;

            // -------------------------------------------------
            var currentPeriodAverage = generalAverage;

            var previousPeriodAverage = await GetAverageGradeAsync(
                previousQuarter.Desde,
                previousQuarter.Hasta,
                includeHomework: true,
                ct);

            // -------------------------------------------------
            // AVERAGE GRADES BY COURSE (GENERAL, CURRENT ACADEMIC QUARTER)
            // -------------------------------------------------
            var averageGradesByCourse = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    !x.Archivado &&
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Fecha >= currentQuarter.Desde &&
                    x.Fecha <= currentDataTo &&
                    (
                        x.Tipo == TipoCalificacion.Homework ||
                        x.Tipo == TipoCalificacion.Quiz ||
                        x.Tipo == TipoCalificacion.Test ||
                        x.Tipo == TipoCalificacion.Participation ||
                        x.Tipo == TipoCalificacion.Behaviour
                    ))
                .GroupBy(x => new { x.CursoId, x.Curso.Nombre, x.Curso.Descripcion })
                .Select(g => new DashboardAverageGradeByCourseModel
                {
                    CursoId = g.Key.CursoId,
                    CursoNombre = g.Key.Nombre,
                    CursoDescripcion = g.Key.Descripcion,
                    AverageGrade = Math.Round(g.Average(x => x.Nota), 2)
                })
                .OrderByDescending(x => x.AverageGrade)
                .ToListAsync(ct);

            foreach (var course in averageGradesByCourse)
            {
                course.ProfesoresNombres = GetCourseTeacherNames(course.CursoId);
            }

            // -------------------------------------------------
            // -------------------------------------------------
            // AVERAGE GRADES BY COURSE (SOLO MANUALES, CURRENT ACADEMIC QUARTER)
            // -------------------------------------------------
            var manualAverageGradesByCourse = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    !x.Archivado &&
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Fecha >= currentQuarter.Desde &&
                    x.Fecha <= currentDataTo &&
                    (
                        x.Tipo == TipoCalificacion.Quiz ||
                        x.Tipo == TipoCalificacion.Test ||
                        x.Tipo == TipoCalificacion.Participation ||
                        x.Tipo == TipoCalificacion.Behaviour
                    ))
                .GroupBy(x => new { x.CursoId, x.Curso.Nombre, x.Curso.Descripcion })
                .Select(g => new DashboardAverageGradeByCourseModel
                {
                    CursoId = g.Key.CursoId,
                    CursoNombre = g.Key.Nombre,
                    CursoDescripcion = g.Key.Descripcion,
                    AverageGrade = Math.Round(g.Average(x => x.Nota), 2)
                })
                .OrderByDescending(x => x.AverageGrade)
                .ToListAsync(ct);

            foreach (var course in manualAverageGradesByCourse)
            {
                course.ProfesoresNombres = GetCourseTeacherNames(course.CursoId);
            }

            var previousAverageGradesByCourse = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    !x.Archivado &&
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Fecha >= previousQuarter.Desde &&
                    x.Fecha <= previousQuarter.Hasta &&
                    (
                        x.Tipo == TipoCalificacion.Homework ||
                        x.Tipo == TipoCalificacion.Quiz ||
                        x.Tipo == TipoCalificacion.Test ||
                        x.Tipo == TipoCalificacion.Participation ||
                        x.Tipo == TipoCalificacion.Behaviour
                    ))
                .GroupBy(x => new { x.CursoId, x.Curso.Nombre, x.Curso.Descripcion })
                .Select(g => new DashboardCourseTrendRiskModel
                {
                    CursoId = g.Key.CursoId,
                    CursoNombre = g.Key.Nombre,
                    CursoDescripcion = g.Key.Descripcion,
                    CurrentValue = 0,
                    PreviousValue = Math.Round(g.Average(x => x.Nota), 2),
                    Delta = 0
                })
                .ToListAsync(ct);

            var coursesWithPerformanceDecline = averageGradesByCourse
                .Join(
                    previousAverageGradesByCourse,
                    current => current.CursoId,
                    previous => previous.CursoId,
                    (current, previous) => new DashboardCourseTrendRiskModel
                    {
                        CursoId = current.CursoId,
                        CursoNombre = current.CursoNombre,
                        CursoDescripcion = current.CursoDescripcion,
                        ProfesoresNombres = current.ProfesoresNombres,
                        Context = "trend",
                        ContextLabel = $"Caida respecto a {previousQuarter.Etiqueta}",
                        PeriodLabel = currentQuarter.Etiqueta,
                        TrendType = "performance-decline",
                        CurrentValue = current.AverageGrade,
                        PreviousValue = previous.PreviousValue,
                        Delta = Math.Round(current.AverageGrade - previous.PreviousValue, 2)
                    })
                .Where(x => x.Delta <= -5)
                .OrderBy(x => x.Delta)
                .Take(5)
                .ToList();

            // -------------------------------------------------
            // STUDENTS AT RISK THIS ACADEMIC QUARTER (GENERAL)
            // promedio trimestral < 60 incluyendo homework + manuales
            // -------------------------------------------------
            var studentsAtRiskThisMonthCount = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    !x.Archivado &&
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Fecha >= currentQuarter.Desde &&
                    x.Fecha <= currentDataTo &&
                    (
                        x.Tipo == TipoCalificacion.Homework ||
                        x.Tipo == TipoCalificacion.Quiz ||
                        x.Tipo == TipoCalificacion.Test ||
                        x.Tipo == TipoCalificacion.Participation ||
                        x.Tipo == TipoCalificacion.Behaviour
                    ))
                .GroupBy(x => x.AlumnoId)
                .Where(g => g.Average(x => x.Nota) < 60)
                .CountAsync(ct);

            var studentAverageGradesByCourse = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    !x.Archivado &&
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Fecha >= currentQuarter.Desde &&
                    x.Fecha <= currentDataTo &&
                    (
                        x.Tipo == TipoCalificacion.Homework ||
                        x.Tipo == TipoCalificacion.Quiz ||
                        x.Tipo == TipoCalificacion.Test ||
                        x.Tipo == TipoCalificacion.Participation ||
                        x.Tipo == TipoCalificacion.Behaviour
                    ))
                .GroupBy(x => new { x.AlumnoId, x.CursoId })
                .Select(g => new
                {
                    g.Key.AlumnoId,
                    g.Key.CursoId,
                    AverageGrade = Math.Round(g.Average(x => x.Nota), 2)
                })
                .ToListAsync(ct);

            var studentAverageGradesByCourseDict = studentAverageGradesByCourse
                .ToDictionary(x => (x.AlumnoId, x.CursoId), x => (decimal?)x.AverageGrade);

            var studentsAtRiskByAverage = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    !x.Archivado &&
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Fecha >= currentQuarter.Desde &&
                    x.Fecha <= currentDataTo &&
                    (
                        x.Tipo == TipoCalificacion.Homework ||
                        x.Tipo == TipoCalificacion.Quiz ||
                        x.Tipo == TipoCalificacion.Test ||
                        x.Tipo == TipoCalificacion.Participation ||
                        x.Tipo == TipoCalificacion.Behaviour
                    ))
                .GroupBy(x => new
                {
                    x.AlumnoId,
                    AlumnoNombre = x.Alumno.Usuario.Nombre + " " + x.Alumno.Usuario.Apellido,
                    AlumnoAvatarUrl = x.Alumno.Usuario.AvatarUrl,
                    x.CursoId,
                    CursoNombre = x.Curso.Nombre,
                    CursoDescripcion = x.Curso.Descripcion
                })
                .Select(g => new DashboardStudentAverageRiskModel
                {
                    AlumnoId = g.Key.AlumnoId,
                    AlumnoNombre = g.Key.AlumnoNombre,
                    AlumnoAvatarUrl = g.Key.AlumnoAvatarUrl,
                    CursoId = g.Key.CursoId,
                    CursoNombre = g.Key.CursoNombre,
                    CursoDescripcion = g.Key.CursoDescripcion,
                    AverageGrade = Math.Round(g.Average(x => x.Nota), 2),
                    CalificacionesCount = g.Count()
                })
                .Where(x => x.AverageGrade < 60)
                .OrderBy(x => x.AverageGrade)
                .Take(8)
                .ToListAsync(ct);

            // -------------------------------------------------
            // LOW MANUAL GRADE BASE QUERY
            // evaluaciones manuales < 60 en el trimestre academico
            // -------------------------------------------------
            var lowManualGradesBaseQuery = _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    !x.Archivado &&
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Fecha >= currentQuarter.Desde &&
                    x.Fecha <= currentDataTo &&
                    x.Nota < 60 &&
                    (
                        x.Tipo == TipoCalificacion.Quiz ||
                        x.Tipo == TipoCalificacion.Test ||
                        x.Tipo == TipoCalificacion.Participation ||
                        x.Tipo == TipoCalificacion.Behaviour
                    ));

            // -------------------------------------------------
            // STUDENTS WITH AT LEAST ONE LOW MANUAL GRADE IN CURRENT ACADEMIC QUARTER
            // -------------------------------------------------
            var studentsManualLowGradesThisMonthCount = await lowManualGradesBaseQuery
                .Select(x => x.AlumnoId)
                .Distinct()
                .CountAsync(ct);

            // -------------------------------------------------
            // STUDENTS MANUAL LOW PERFORMANCE LIST
            // listado de evaluaciones bajas reales, no promedio
            // -------------------------------------------------
            var studentsManualLowPerformance = await lowManualGradesBaseQuery
                .OrderBy(x => x.Nota)
                .ThenByDescending(x => x.Fecha)
                .Take(5)
                .Select(x => new DashboardLowManualGradeAlertModel
                {
                    AlumnoId = x.AlumnoId,
                    AlumnoNombre = x.Alumno.Usuario.Nombre + " " + x.Alumno.Usuario.Apellido,
                    AlumnoAvatarUrl = x.Alumno.Usuario.AvatarUrl,
                    CursoId = x.CursoId,
                    CursoNombre = x.Curso.Nombre,
                    CursoDescripcion = x.Curso.Descripcion,
                    CalificacionId = x.Id,
                    Titulo = x.Titulo,
                    Tipo = x.Tipo,
                    Nota = Math.Round(x.Nota, 2),
                    Fecha = x.Fecha
                })
                .ToListAsync(ct);

            // -------------------------------------------------
            // COURSES AT RISK BY OVERALL AVERAGE
            // -------------------------------------------------
            var coursesAtRiskByOverallAverage = averageGradesByCourse
                .Where(x => x.AverageGrade < 60)
                .OrderBy(x => x.AverageGrade)
                .Take(5)
                .ToList();

            // -------------------------------------------------
            // COURSES AT RISK BY MANUAL AVERAGE
            // sin contar homework
            // -------------------------------------------------
            var coursesAtRiskByManualAverage = manualAverageGradesByCourse
                .Where(x => x.AverageGrade < 60)
                .OrderBy(x => x.AverageGrade)
                .Take(5)
                .ToList();

            // -------------------------------------------------
            // ATTENDANCE HEALTH (CURRENT ACADEMIC QUARTER)
            // -------------------------------------------------
            var periodClasses = await _db.Clases
                .AsNoTracking()
                .Where(x =>
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha >= currentQuarter.Desde &&
                    x.Fecha <= currentDataTo)
                .Select(x => new
                {
                    x.Id,
                    x.CursoId,
                    CursoNombre = x.Curso.Nombre,
                    CursoDescripcion = x.Curso.Descripcion
                })
                .ToListAsync(ct);

            foreach (var student in studentsManualLowPerformance)
            {
                student.AverageGrade = studentAverageGradesByCourseDict.GetValueOrDefault(
                    (student.AlumnoId, student.CursoId));
            }

            var periodClassIds = periodClasses.Select(x => x.Id).ToList();

            var activeMatriculas = await _db.Matriculas
                .AsNoTracking()
                .Where(x => x.Curso.Estado == EstadoCurso.Activo)
                .Select(x => new
                {
                    x.CursoId,
                    CursoNombre = x.Curso.Nombre,
                    CursoDescripcion = x.Curso.Descripcion,
                    x.AlumnoId,
                    AlumnoNombre = x.Alumno.Usuario.Nombre + " " + x.Alumno.Usuario.Apellido,
                    AlumnoAvatarUrl = x.Alumno.Usuario.AvatarUrl
                })
                .ToListAsync(ct);

            var periodAttendances = await _db.Asistencias
                .AsNoTracking()
                .Where(x => periodClassIds.Contains(x.ClaseId))
                .Select(x => new
                {
                    x.AlumnoId,
                    x.Clase.CursoId,
                    x.Clase.Fecha,
                    x.Estado
                })
                .ToListAsync(ct);

            var classCountByCourse = periodClasses
                .GroupBy(x => new { x.CursoId, x.CursoNombre, x.CursoDescripcion })
                .ToDictionary(g => g.Key.CursoId, g => new
                {
                    g.Key.CursoNombre,
                    g.Key.CursoDescripcion,
                    Count = g.Count()
                });

            var studentCountByCourse = activeMatriculas
                .GroupBy(x => x.CursoId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.AlumnoId).Distinct().Count());

            var attendanceByCourse = periodAttendances
                .GroupBy(x => x.CursoId)
                .ToDictionary(g => g.Key, g => new
                {
                    Presentes = g.Count(x => x.Estado == EstadoAsistencia.Presente),
                    Ausentes = g.Count(x => x.Estado == EstadoAsistencia.Ausente)
                });

            var totalExpectedAttendanceRecords = classCountByCourse.Sum(x =>
                x.Value.Count * studentCountByCourse.GetValueOrDefault(x.Key));
            var totalPresentes = attendanceByCourse.Sum(x => x.Value.Presentes);

            var institutionalAttendanceAverage = totalExpectedAttendanceRecords > 0
                ? Math.Round((decimal)totalPresentes * 100 / totalExpectedAttendanceRecords, 2)
                : (decimal?)null;

            var previousPeriodAttendanceAverage = await GetInstitutionalAttendanceAverageAsync(
                previousQuarter.Desde,
                previousQuarter.Hasta,
                ct);

            var coursesAtRiskByAttendance = classCountByCourse
                .Select(x =>
                {
                    var studentsInCourse = studentCountByCourse.GetValueOrDefault(x.Key);
                    var expectedRecords = x.Value.Count * studentsInCourse;
                    attendanceByCourse.TryGetValue(x.Key, out var attendance);
                    var present = attendance?.Presentes ?? 0;
                    var absent = attendance?.Ausentes ?? 0;
                    var percentage = expectedRecords > 0
                        ? Math.Round((decimal)present * 100 / expectedRecords, 2)
                        : 0;

                    return new DashboardCourseAttendanceRiskModel
                    {
                        CursoId = x.Key,
                        CursoNombre = x.Value.CursoNombre,
                        CursoDescripcion = x.Value.CursoDescripcion,
                        AttendancePercentage = percentage,
                        Ausentes = absent,
                        ExpectedAttendanceRecords = expectedRecords
                    };
                })
                .Where(x => x.ExpectedAttendanceRecords > 0 && x.AttendancePercentage < 70)
                .OrderBy(x => x.AttendancePercentage)
                .Take(5)
                .ToList();

            foreach (var course in coursesAtRiskByAttendance)
            {
                course.ProfesoresNombres = GetCourseTeacherNames(course.CursoId);
            }

            var currentAttendanceByCourse = classCountByCourse
                .Select(x =>
                {
                    var studentsInCourse = studentCountByCourse.GetValueOrDefault(x.Key);
                    var expectedRecords = x.Value.Count * studentsInCourse;
                    attendanceByCourse.TryGetValue(x.Key, out var attendance);
                    var present = attendance?.Presentes ?? 0;
                    var percentage = expectedRecords > 0
                        ? Math.Round((decimal)present * 100 / expectedRecords, 2)
                        : (decimal?)null;

                    return new
                    {
                        CursoId = x.Key,
                        x.Value.CursoNombre,
                        x.Value.CursoDescripcion,
                        AttendancePercentage = percentage
                    };
                })
                .Where(x => x.AttendancePercentage.HasValue)
                .ToList();

            var previousClasses = await _db.Clases
                .AsNoTracking()
                .Where(x =>
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha >= previousQuarter.Desde &&
                    x.Fecha <= previousQuarter.Hasta)
                .Select(x => new
                {
                    x.Id,
                    x.CursoId,
                    CursoNombre = x.Curso.Nombre,
                    CursoDescripcion = x.Curso.Descripcion
                })
                .ToListAsync(ct);

            var previousClassIds = previousClasses.Select(x => x.Id).ToList();
            var previousAttendances = await _db.Asistencias
                .AsNoTracking()
                .Where(x => previousClassIds.Contains(x.ClaseId))
                .Select(x => new
                {
                    x.AlumnoId,
                    x.Clase.CursoId,
                    x.Estado
                })
                .ToListAsync(ct);

            var previousClassCountByCourse = previousClasses
                .GroupBy(x => new { x.CursoId, x.CursoNombre, x.CursoDescripcion })
                .ToDictionary(g => g.Key.CursoId, g => new
                {
                    g.Key.CursoNombre,
                    g.Key.CursoDescripcion,
                    Count = g.Count()
                });

            var previousAttendanceByCourse = previousAttendances
                .GroupBy(x => x.CursoId)
                .ToDictionary(g => g.Key, g => g.Count(x => x.Estado == EstadoAsistencia.Presente));

            var previousAttendanceRateByCourse = previousClassCountByCourse
                .Select(x =>
                {
                    var studentsInCourse = studentCountByCourse.GetValueOrDefault(x.Key);
                    var expectedRecords = x.Value.Count * studentsInCourse;
                    var present = previousAttendanceByCourse.GetValueOrDefault(x.Key);
                    var percentage = expectedRecords > 0
                        ? Math.Round((decimal)present * 100 / expectedRecords, 2)
                        : (decimal?)null;

                    return new
                    {
                        CursoId = x.Key,
                        x.Value.CursoNombre,
                        x.Value.CursoDescripcion,
                        AttendancePercentage = percentage
                    };
                })
                .Where(x => x.AttendancePercentage.HasValue)
                .ToList();

            var coursesWithAttendanceDecline = currentAttendanceByCourse
                .Join(
                    previousAttendanceRateByCourse,
                    current => current.CursoId,
                    previous => previous.CursoId,
                    (current, previous) => new DashboardCourseTrendRiskModel
                    {
                        CursoId = current.CursoId,
                        CursoNombre = current.CursoNombre,
                        CursoDescripcion = current.CursoDescripcion,
                        ProfesoresNombres = GetCourseTeacherNames(current.CursoId),
                        Context = "trend",
                        ContextLabel = $"Caida respecto a {previousQuarter.Etiqueta}",
                        PeriodLabel = currentQuarter.Etiqueta,
                        TrendType = "attendance-decline",
                        CurrentValue = current.AttendancePercentage!.Value,
                        PreviousValue = previous.AttendancePercentage!.Value,
                        Delta = Math.Round(current.AttendancePercentage.Value - previous.AttendancePercentage.Value, 2)
                    })
                .Where(x => x.Delta <= -5)
                .OrderBy(x => x.Delta)
                .Take(5)
                .ToList();

            var attendanceByStudentCourse = periodAttendances
                .GroupBy(x => new { x.AlumnoId, x.CursoId })
                .ToDictionary(g => (g.Key.AlumnoId, g.Key.CursoId), g => new
                {
                    Presentes = g.Count(x => x.Estado == EstadoAsistencia.Presente),
                    Ausentes = g.Count(x => x.Estado == EstadoAsistencia.Ausente)
                });

            var rollingClasses = await _db.Clases
                .AsNoTracking()
                .Where(x =>
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha >= consecutiveAbsenceFrom &&
                    x.Fecha <= today)
                .Select(x => new
                {
                    x.Id,
                    x.CursoId
                })
                .ToListAsync(ct);

            var rollingClassIds = rollingClasses.Select(x => x.Id).ToList();
            var rollingAttendances = await _db.Asistencias
                .AsNoTracking()
                .Where(x => rollingClassIds.Contains(x.ClaseId))
                .Select(x => new
                {
                    x.AlumnoId,
                    x.Clase.CursoId,
                    x.Clase.Fecha,
                    x.Estado
                })
                .ToListAsync(ct);

            var rollingClassCountByCourse = rollingClasses
                .GroupBy(x => x.CursoId)
                .ToDictionary(g => g.Key, g => g.Count());

            var rollingAttendanceByStudentCourse = rollingAttendances
                .GroupBy(x => new { x.AlumnoId, x.CursoId })
                .ToDictionary(g => (g.Key.AlumnoId, g.Key.CursoId), g => new
                {
                    Presentes = g.Count(x => x.Estado == EstadoAsistencia.Presente)
                });

            var studentsWithMultipleAbsences = activeMatriculas
                .Select(x =>
                {
                    var classCount = classCountByCourse.TryGetValue(x.CursoId, out var classInfo)
                        ? classInfo.Count
                        : 0;

                    attendanceByStudentCourse.TryGetValue((x.AlumnoId, x.CursoId), out var attendance);
                    var presentes = attendance?.Presentes ?? 0;
                    var ausentes = attendance?.Ausentes ?? 0;
                    var percentage = classCount > 0
                        ? Math.Round((decimal)presentes * 100 / classCount, 2)
                        : 0;

                    return new DashboardStudentAttendanceRiskModel
                    {
                        AlumnoId = x.AlumnoId,
                        AlumnoNombre = x.AlumnoNombre,
                        AlumnoAvatarUrl = x.AlumnoAvatarUrl,
                        CursoId = x.CursoId,
                        CursoNombre = x.CursoNombre,
                        CursoDescripcion = x.CursoDescripcion,
                        Ausentes = ausentes,
                        ClasesTotales = classCount,
                        AttendancePercentage = percentage,
                        AverageGrade = studentAverageGradesByCourseDict.GetValueOrDefault((x.AlumnoId, x.CursoId))
                    };
                })
                .Where(x =>
                    x.ClasesTotales > 0 &&
                    x.AttendancePercentage < 70)
                .OrderByDescending(x => x.Ausentes)
                .ThenBy(x => x.AttendancePercentage)
                .Take(8)
                .ToList();

            var activeMatriculaByStudentCourse = activeMatriculas
                .GroupBy(x => new { x.AlumnoId, x.CursoId })
                .ToDictionary(g => (g.Key.AlumnoId, g.Key.CursoId), g => g.First());

            var attendancePercentageByStudentCourse = studentsWithMultipleAbsences
                .ToDictionary(x => (x.AlumnoId, x.CursoId), x => x.AttendancePercentage);

            var studentsWithConsecutiveAbsences = rollingAttendances
                .Where(x => x.Estado == EstadoAsistencia.Presente || x.Estado == EstadoAsistencia.Ausente)
                .GroupBy(x => new { x.AlumnoId, x.CursoId })
                .Select(g =>
                {
                    var currentStreak = 0;
                    var maxStreak = 0;
                    DateOnly? lastAbsenceDate = null;

                    foreach (var attendance in g.OrderBy(x => x.Fecha))
                    {
                        if (attendance.Estado == EstadoAsistencia.Ausente)
                        {
                            currentStreak++;
                            if (currentStreak >= maxStreak)
                            {
                                maxStreak = currentStreak;
                                lastAbsenceDate = attendance.Fecha;
                            }
                        }
                        else
                        {
                            currentStreak = 0;
                        }
                    }

                    if (!activeMatriculaByStudentCourse.TryGetValue((g.Key.AlumnoId, g.Key.CursoId), out var matricula) ||
                        !lastAbsenceDate.HasValue)
                    {
                        return null;
                    }

                    rollingAttendanceByStudentCourse.TryGetValue((g.Key.AlumnoId, g.Key.CursoId), out var attendanceSummary);
                    var classCount = rollingClassCountByCourse.TryGetValue(g.Key.CursoId, out var rollingClassCount)
                        ? rollingClassCount
                        : 0;
                    var attendancePercentage = classCount > 0
                        ? Math.Round((decimal)(attendanceSummary?.Presentes ?? 0) * 100 / classCount, 2)
                        : 0;

                    return new DashboardStudentConsecutiveAbsenceRiskModel
                    {
                        AlumnoId = matricula.AlumnoId,
                        AlumnoNombre = matricula.AlumnoNombre,
                        AlumnoAvatarUrl = matricula.AlumnoAvatarUrl,
                        CursoId = matricula.CursoId,
                        CursoNombre = matricula.CursoNombre,
                        CursoDescripcion = matricula.CursoDescripcion,
                        ConsecutiveAbsences = maxStreak,
                        LastAbsenceDate = lastAbsenceDate.Value,
                        AttendancePercentage = attendancePercentage,
                        AverageGrade = studentAverageGradesByCourseDict.GetValueOrDefault((matricula.AlumnoId, matricula.CursoId))
                    };
                })
                .Where(x => x != null && x.ConsecutiveAbsences >= 2)
                .OrderByDescending(x => x!.ConsecutiveAbsences)
                .ThenBy(x => x!.AttendancePercentage)
                .Take(8)
                .Select(x => x!)
                .ToList();

            var studentsWithCombinedAcademicRisk = studentsAtRiskByAverage
                .Where(student =>
                    attendancePercentageByStudentCourse.TryGetValue(
                        (student.AlumnoId, student.CursoId),
                        out var attendancePercentage) &&
                    attendancePercentage < 70)
                .Select(student =>
                {
                    var attendanceRisk = studentsWithMultipleAbsences
                        .First(x => x.AlumnoId == student.AlumnoId && x.CursoId == student.CursoId);

                    return new DashboardStudentCombinedRiskModel
                    {
                        AlumnoId = student.AlumnoId,
                        AlumnoNombre = student.AlumnoNombre,
                        AlumnoAvatarUrl = student.AlumnoAvatarUrl,
                        CursoId = student.CursoId,
                        CursoNombre = student.CursoNombre,
                        CursoDescripcion = student.CursoDescripcion,
                        AverageGrade = student.AverageGrade,
                        AttendancePercentage = attendanceRisk.AttendancePercentage,
                        Absences = attendanceRisk.Ausentes
                    };
                })
                .OrderBy(x => x.AverageGrade)
                .ThenBy(x => x.AttendancePercentage)
                .Take(8)
                .ToList();

            // -------------------------------------------------
            // HOMEWORK HEALTH (CURRENT ACADEMIC QUARTER TO DATE)
            // -------------------------------------------------
            var pendingHomeworkByCourse = await _db.Entregas
                .AsNoTracking()
                .Where(x =>
                    x.Tarea.Curso.Estado == EstadoCurso.Activo &&
                    x.Tarea.Estado == EstadoTarea.Publicada &&
                    x.Tarea.FechaEntregaUtc.HasValue &&
                    x.Tarea.FechaEntregaUtc.Value >= periodFromUtc &&
                    x.Tarea.FechaEntregaUtc.Value < periodToUtcExclusive &&
                    !x.Feedbacks.Any(f => f.EsVigente))
                .GroupBy(x => new
                {
                    x.Tarea.CursoId,
                    CursoNombre = x.Tarea.Curso.Nombre,
                    CursoDescripcion = x.Tarea.Curso.Descripcion
                })
                .Select(g => new
                {
                    g.Key.CursoId,
                    g.Key.CursoNombre,
                    g.Key.CursoDescripcion,
                    Count = g.Count()
                })
                .ToListAsync(ct);

            var institutionalHomeworkPendingCorrectionCount = pendingHomeworkByCourse.Sum(x => x.Count);

            var pendingHomeworkByCourseDict = pendingHomeworkByCourse.ToDictionary(x => x.CursoId, x => x.Count);
            var averageByCourseDict = averageGradesByCourse.ToDictionary(x => x.CursoId, x => (decimal?)x.AverageGrade);
            var attendanceRiskByCourseDict = coursesAtRiskByAttendance.ToDictionary(x => x.CursoId, x => (decimal?)x.AttendancePercentage);
            var studentsAtRiskByCourseDict = studentsAtRiskByAverage
                .Select(x => new { x.CursoId, x.AlumnoId })
                .Concat(studentsWithMultipleAbsences.Select(x => new { x.CursoId, x.AlumnoId }))
                .Concat(studentsWithConsecutiveAbsences.Select(x => new { x.CursoId, x.AlumnoId }))
                .Concat(studentsWithCombinedAcademicRisk.Select(x => new { x.CursoId, x.AlumnoId }))
                .GroupBy(x => x.CursoId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(student => student.AlumnoId).Distinct().Count());

            var criticalCourseIds = coursesAtRiskByOverallAverage.Select(x => x.CursoId)
                .Concat(coursesAtRiskByManualAverage.Select(x => x.CursoId))
                .Concat(coursesAtRiskByAttendance.Select(x => x.CursoId))
                .Concat(pendingHomeworkByCourse.Where(x => x.Count >= 5).Select(x => x.CursoId))
                .Distinct()
                .ToList();

            var courseNamesById = activeMatriculas
                .GroupBy(x => x.CursoId)
                .ToDictionary(g => g.Key, g => g.First().CursoNombre);
            var courseDescriptionsById = activeMatriculas
                .GroupBy(x => x.CursoId)
                .ToDictionary(g => g.Key, g => g.First().CursoDescripcion);

            foreach (var course in averageGradesByCourse)
            {
                courseNamesById.TryAdd(course.CursoId, course.CursoNombre);
                courseDescriptionsById.TryAdd(course.CursoId, course.CursoDescripcion);
            }

            foreach (var course in coursesAtRiskByAttendance)
            {
                courseNamesById.TryAdd(course.CursoId, course.CursoNombre);
                courseDescriptionsById.TryAdd(course.CursoId, course.CursoDescripcion);
            }

            foreach (var course in pendingHomeworkByCourse)
            {
                courseNamesById.TryAdd(course.CursoId, course.CursoNombre);
                courseDescriptionsById.TryAdd(course.CursoId, course.CursoDescripcion);
            }

            foreach (var course in coursesWithPerformanceDecline)
            {
                courseNamesById.TryAdd(course.CursoId, course.CursoNombre);
                courseDescriptionsById.TryAdd(course.CursoId, course.CursoDescripcion);
            }

            foreach (var course in coursesWithAttendanceDecline)
            {
                courseNamesById.TryAdd(course.CursoId, course.CursoNombre);
                courseDescriptionsById.TryAdd(course.CursoId, course.CursoDescripcion);
            }

            var previousAttendanceRateByCourseDict = previousAttendanceRateByCourse
                .ToDictionary(x => x.CursoId, x => x.AttendancePercentage);
            var previousAverageByCourseDict = previousAverageGradesByCourse
                .ToDictionary(x => x.CursoId, x => (decimal?)x.PreviousValue);
            var pendingFollowUpCourseIds = previousAverageByCourseDict
                .Where(x => x.Value.HasValue && x.Value.Value < 75m)
                .Select(x => x.Key)
                .Concat(previousAttendanceRateByCourseDict
                    .Where(x => x.Value.HasValue && x.Value.Value < 85m)
                    .Select(x => x.Key))
                .Distinct()
                .ToList();

            var coursesPendingFollowUp = pendingFollowUpCourseIds
                .Select(courseId =>
                {
                    var average = previousAverageByCourseDict.GetValueOrDefault(courseId);
                    var attendance = previousAttendanceRateByCourseDict.GetValueOrDefault(courseId);
                    var reasons = new List<string>();

                    if (average.HasValue && average.Value < 60m)
                        reasons.Add($"Promedio bajo {average:0.##}");
                    else if (average.HasValue && average.Value < 75m)
                        reasons.Add($"Promedio en seguimiento {average:0.##}");

                    if (attendance.HasValue && attendance.Value < 70m)
                        reasons.Add($"Baja asistencia {attendance:0.##}%");
                    else if (attendance.HasValue && attendance.Value < 85m)
                        reasons.Add($"Asistencia en seguimiento {attendance:0.##}%");

                    var isCritical =
                        (average.HasValue && average.Value < 60m) ||
                        (attendance.HasValue && attendance.Value < 70m);
                    var reason = string.Join(" y ", reasons);

                    return new DashboardCoursePendingFollowUpModel
                    {
                        CursoId = courseId,
                        CursoNombre = courseNamesById.GetValueOrDefault(courseId, "Curso"),
                        CursoDescripcion = courseDescriptionsById.GetValueOrDefault(courseId),
                        ProfesoresNombres = GetCourseTeacherNames(courseId),
                        Context = "pending-follow-up",
                        ContextLabel = "Seguimiento pendiente del trimestre anterior",
                        PeriodLabel = previousQuarter.Etiqueta,
                        QuarterNumber = previousQuarter.Trimestre,
                        Year = previousQuarter.Anio,
                        Level = isCritical ? CourseHealthLevels.Critical : CourseHealthLevels.FollowUp,
                        Reason = reason,
                        AverageGrade = average,
                        AttendancePercentage = attendance,
                        Description = $"{(isCritical ? "Critico" : "Seguimiento")} en {previousQuarter.Etiqueta}: {reason}"
                    };
                })
                .OrderByDescending(x => x.Level == CourseHealthLevels.Critical)
                .ThenBy(x => x.AverageGrade ?? 100m)
                .ThenBy(x => x.AttendancePercentage ?? 100m)
                .Take(8)
                .ToList();

            var courseTrendAlerts = coursesWithPerformanceDecline
                .Concat(coursesWithAttendanceDecline)
                .OrderBy(x => x.Delta)
                .Take(8)
                .ToList();

            var criticalCourses = criticalCourseIds
                .Select(courseId =>
                {
                    var signals = 0;
                    if (coursesAtRiskByOverallAverage.Any(x => x.CursoId == courseId)) signals++;
                    if (coursesAtRiskByManualAverage.Any(x => x.CursoId == courseId)) signals++;
                    if (coursesAtRiskByAttendance.Any(x => x.CursoId == courseId)) signals++;
                    if (pendingHomeworkByCourseDict.GetValueOrDefault(courseId) >= 5) signals++;

                    var teacherNames = GetCourseTeacherNames(courseId);
                    var attendanceAverage = attendanceRiskByCourseDict.GetValueOrDefault(courseId);
                    var academicAverage = averageByCourseDict.GetValueOrDefault(courseId);
                    var studentsAtRiskCount = studentsAtRiskByCourseDict.GetValueOrDefault(courseId);

                    return new DashboardCriticalCourseModel
                    {
                        CursoId = courseId,
                        CursoNombre = courseNamesById.GetValueOrDefault(courseId, "Curso"),
                        CursoDescripcion = courseDescriptionsById.GetValueOrDefault(courseId),
                        ProfesoresNombres = teacherNames,
                        AverageGrade = academicAverage,
                        AttendancePercentage = attendanceAverage,
                        PendingCorrectionCount = pendingHomeworkByCourseDict.GetValueOrDefault(courseId),
                        SignalsCount = signals,
                        Context = "current-risk",
                        ContextLabel = $"Riesgo actual en {currentQuarter.Etiqueta}",
                        PeriodLabel = currentQuarter.Etiqueta,
                        StudentsAtRiskCurrentCount = studentsAtRiskCount,
                        PendingFollowUpCount = coursesPendingFollowUp.Count(x => x.CursoId == courseId),
                        Health = CourseHealthCalculator.Calculate(
                            attendanceAverage,
                            academicAverage,
                            studentsAtRiskCount,
                            teacherNames.Count > 0)
                    };
                })
                .OrderByDescending(x => x.SignalsCount)
                .ThenBy(x => x.AverageGrade ?? 100)
                .Take(5)
                .ToList();

            var currentStudentRiskKeys = studentsAtRiskByAverage
                .Select(x => (x.AlumnoId, x.CursoId))
                .Concat(studentsWithMultipleAbsences.Select(x => (x.AlumnoId, x.CursoId)))
                .Concat(studentsWithConsecutiveAbsences.Select(x => (x.AlumnoId, x.CursoId)))
                .Concat(studentsWithCombinedAcademicRisk.Select(x => (x.AlumnoId, x.CursoId)))
                .Distinct()
                .ToHashSet();
            var currentCourseRiskIds = criticalCourses
                .Select(x => x.CursoId)
                .ToHashSet();
            var monitoringPeriods = currentQuarter.Trimestre > 1
                ? Enumerable.Range(1, currentQuarter.Trimestre - 1)
                    .Select(quarter => PeriodoAcademicoHelper.ObtenerTrimestre(currentQuarter.Anio, quarter))
                    .ToList()
                : new List<PeriodoAcademicoTrimestre> { previousQuarter };
            var openFollowUpsByKey = new Dictionary<string, DashboardOpenFollowUpModel>();

            foreach (var monitoringPeriod in monitoringPeriods)
            {
                var monitoringStudentAverageRows = await _db.Calificaciones
                    .AsNoTracking()
                    .Where(x =>
                        !x.Archivado &&
                        x.Curso.Estado == EstadoCurso.Activo &&
                        x.Fecha >= monitoringPeriod.Desde &&
                        x.Fecha <= monitoringPeriod.Hasta &&
                        (
                            x.Tipo == TipoCalificacion.Homework ||
                            x.Tipo == TipoCalificacion.Quiz ||
                            x.Tipo == TipoCalificacion.Test ||
                            x.Tipo == TipoCalificacion.Participation ||
                            x.Tipo == TipoCalificacion.Behaviour
                        ))
                    .GroupBy(x => new
                    {
                        x.AlumnoId,
                        AlumnoNombre = x.Alumno.Usuario.Nombre + " " + x.Alumno.Usuario.Apellido,
                        AlumnoAvatarUrl = x.Alumno.Usuario.AvatarUrl,
                        x.CursoId,
                        CursoNombre = x.Curso.Nombre,
                        CursoDescripcion = x.Curso.Descripcion
                    })
                    .Select(g => new
                    {
                        g.Key.AlumnoId,
                        g.Key.AlumnoNombre,
                        g.Key.AlumnoAvatarUrl,
                        g.Key.CursoId,
                        g.Key.CursoNombre,
                        g.Key.CursoDescripcion,
                        AverageGrade = Math.Round(g.Average(x => x.Nota), 2)
                    })
                    .Where(x => x.AverageGrade < 60m)
                    .ToListAsync(ct);

                foreach (var row in monitoringStudentAverageRows)
                {
                    if (currentStudentRiskKeys.Contains((row.AlumnoId, row.CursoId)))
                        continue;

                    var key = $"student-{row.AlumnoId}-{row.CursoId}-{monitoringPeriod.Anio}-{monitoringPeriod.Trimestre}";

                    if (!openFollowUpsByKey.TryGetValue(key, out var item))
                    {
                        item = new DashboardOpenFollowUpModel
                        {
                            Id = key,
                            EntityType = "student",
                            EntityId = row.AlumnoId,
                            AlumnoId = row.AlumnoId,
                            AlumnoNombre = row.AlumnoNombre,
                            AlumnoAvatarUrl = row.AlumnoAvatarUrl,
                            CursoId = row.CursoId,
                            CursoNombre = row.CursoNombre,
                            CursoDescripcion = row.CursoDescripcion,
                            PeriodLabel = monitoringPeriod.Etiqueta,
                            QuarterNumber = monitoringPeriod.Trimestre,
                            Year = monitoringPeriod.Anio,
                            Source = "low-average",
                            Level = CourseHealthLevels.Critical,
                            Href = $"/admin/dashboard/students/{row.AlumnoId}/profile"
                        };
                        openFollowUpsByKey[key] = item;
                    }

                    item.AverageGrade = row.AverageGrade;
                    item.Level = CourseHealthLevels.Critical;
                    item.Source = item.AttendancePercentage.HasValue
                        ? "combined-academic-risk"
                        : "low-average";
                    item.Reason = item.AttendancePercentage.HasValue
                        ? $"Promedio {row.AverageGrade:0.##} y asistencia {item.AttendancePercentage:0.##}% en {monitoringPeriod.Etiqueta}"
                        : $"Promedio {row.AverageGrade:0.##} en {monitoringPeriod.Etiqueta}";
                }

                var monitoringClasses = await _db.Clases
                    .AsNoTracking()
                    .Where(x =>
                        x.Curso.Estado == EstadoCurso.Activo &&
                        x.Estado != EstadoClase.Cancelada &&
                        x.Fecha >= monitoringPeriod.Desde &&
                        x.Fecha <= monitoringPeriod.Hasta)
                    .Select(x => new
                    {
                        x.Id,
                        x.CursoId,
                        CursoNombre = x.Curso.Nombre,
                        CursoDescripcion = x.Curso.Descripcion
                    })
                    .ToListAsync(ct);
                var monitoringClassIds = monitoringClasses.Select(x => x.Id).ToList();
                var monitoringAttendances = await _db.Asistencias
                    .AsNoTracking()
                    .Where(x => monitoringClassIds.Contains(x.ClaseId))
                    .Select(x => new
                    {
                        x.AlumnoId,
                        x.Clase.CursoId,
                        x.Estado
                    })
                    .ToListAsync(ct);
                var monitoringClassCountByCourse = monitoringClasses
                    .GroupBy(x => new { x.CursoId, x.CursoNombre, x.CursoDescripcion })
                    .ToDictionary(g => g.Key.CursoId, g => new
                    {
                        g.Key.CursoNombre,
                        g.Key.CursoDescripcion,
                        Count = g.Count()
                    });
                var monitoringAttendanceByStudentCourse = monitoringAttendances
                    .GroupBy(x => new { x.AlumnoId, x.CursoId })
                    .ToDictionary(g => (g.Key.AlumnoId, g.Key.CursoId), g => new
                    {
                        Presentes = g.Count(x => x.Estado == EstadoAsistencia.Presente),
                        Ausentes = g.Count(x => x.Estado == EstadoAsistencia.Ausente)
                    });

                foreach (var row in activeMatriculas)
                {
                    if (currentStudentRiskKeys.Contains((row.AlumnoId, row.CursoId)))
                        continue;

                    var classCount = monitoringClassCountByCourse.TryGetValue(row.CursoId, out var classInfo)
                        ? classInfo.Count
                        : 0;

                    if (classCount == 0)
                        continue;

                    monitoringAttendanceByStudentCourse.TryGetValue((row.AlumnoId, row.CursoId), out var attendance);
                    var attendancePercentage = Math.Round((decimal)(attendance?.Presentes ?? 0) * 100 / classCount, 2);

                    if (attendancePercentage >= 70m)
                        continue;

                    var key = $"student-{row.AlumnoId}-{row.CursoId}-{monitoringPeriod.Anio}-{monitoringPeriod.Trimestre}";

                    if (!openFollowUpsByKey.TryGetValue(key, out var item))
                    {
                        item = new DashboardOpenFollowUpModel
                        {
                            Id = key,
                            EntityType = "student",
                            EntityId = row.AlumnoId,
                            AlumnoId = row.AlumnoId,
                            AlumnoNombre = row.AlumnoNombre,
                            AlumnoAvatarUrl = row.AlumnoAvatarUrl,
                            CursoId = row.CursoId,
                            CursoNombre = row.CursoNombre,
                            CursoDescripcion = row.CursoDescripcion,
                            PeriodLabel = monitoringPeriod.Etiqueta,
                            QuarterNumber = monitoringPeriod.Trimestre,
                            Year = monitoringPeriod.Anio,
                            Source = "low-attendance",
                            Level = CourseHealthLevels.Critical,
                            Href = $"/admin/dashboard/students/{row.AlumnoId}/profile"
                        };
                        openFollowUpsByKey[key] = item;
                    }

                    item.AttendancePercentage = attendancePercentage;
                    item.Level = CourseHealthLevels.Critical;
                    item.Source = item.AverageGrade.HasValue
                        ? "combined-academic-risk"
                        : "low-attendance";
                    item.Reason = item.AverageGrade.HasValue
                        ? $"Promedio {item.AverageGrade:0.##} y asistencia {attendancePercentage:0.##}% en {monitoringPeriod.Etiqueta}"
                        : $"Asistencia critica {attendancePercentage:0.##}% en {monitoringPeriod.Etiqueta}";
                }

                var monitoringLowManualGradeRows = await _db.Calificaciones
                    .AsNoTracking()
                    .Where(x =>
                        !x.Archivado &&
                        x.Curso.Estado == EstadoCurso.Activo &&
                        x.Fecha >= monitoringPeriod.Desde &&
                        x.Fecha <= monitoringPeriod.Hasta &&
                        x.Nota < 60m &&
                        (
                            x.Tipo == TipoCalificacion.Quiz ||
                            x.Tipo == TipoCalificacion.Test ||
                            x.Tipo == TipoCalificacion.Participation ||
                            x.Tipo == TipoCalificacion.Behaviour
                        ))
                    .Select(x => new
                    {
                        x.AlumnoId,
                        AlumnoNombre = x.Alumno.Usuario.Nombre + " " + x.Alumno.Usuario.Apellido,
                        AlumnoAvatarUrl = x.Alumno.Usuario.AvatarUrl,
                        x.CursoId,
                        CursoNombre = x.Curso.Nombre,
                        CursoDescripcion = x.Curso.Descripcion,
                        CalificacionId = x.Id,
                        x.Titulo,
                        x.Tipo,
                        Nota = Math.Round(x.Nota, 2),
                        x.Fecha
                    })
                    .OrderBy(x => x.Nota)
                    .ThenByDescending(x => x.Fecha)
                    .ToListAsync(ct);

                foreach (var row in monitoringLowManualGradeRows)
                {
                    if (currentStudentRiskKeys.Contains((row.AlumnoId, row.CursoId)))
                        continue;

                    var key = $"student-{row.AlumnoId}-{row.CursoId}-{monitoringPeriod.Anio}-{monitoringPeriod.Trimestre}";

                    if (!openFollowUpsByKey.TryGetValue(key, out var item))
                    {
                        item = new DashboardOpenFollowUpModel
                        {
                            Id = key,
                            EntityType = "student",
                            EntityId = row.AlumnoId,
                            AlumnoId = row.AlumnoId,
                            AlumnoNombre = row.AlumnoNombre,
                            AlumnoAvatarUrl = row.AlumnoAvatarUrl,
                            CursoId = row.CursoId,
                            CursoNombre = row.CursoNombre,
                            CursoDescripcion = row.CursoDescripcion,
                            PeriodLabel = monitoringPeriod.Etiqueta,
                            QuarterNumber = monitoringPeriod.Trimestre,
                            Year = monitoringPeriod.Anio,
                            Source = "low-manual-grade",
                            Level = row.Nota < 50m ? CourseHealthLevels.Critical : CourseHealthLevels.FollowUp,
                            Href = $"/admin/dashboard/students/{row.AlumnoId}/profile"
                        };
                        openFollowUpsByKey[key] = item;
                    }

                    if (item.GradeAlerts.All(x => x.CalificacionId != row.CalificacionId))
                    {
                        item.GradeAlerts.Add(new DashboardOpenFollowUpGradeAlertModel
                        {
                            CalificacionId = row.CalificacionId,
                            Titulo = row.Titulo,
                            Tipo = row.Tipo,
                            Nota = row.Nota,
                            Fecha = row.Fecha
                        });
                    }

                    if (row.Nota < 50m)
                    {
                        item.Level = CourseHealthLevels.Critical;
                    }

                    item.Source = item.AverageGrade.HasValue || item.AttendancePercentage.HasValue
                        ? "combined-academic-risk"
                        : "low-manual-grade";

                    if (string.IsNullOrWhiteSpace(item.Reason))
                    {
                        item.Reason = $"{GetTipoCalificacionLabel(row.Tipo)}: {row.Titulo} ({row.Nota:0.##}) en {monitoringPeriod.Etiqueta}";
                    }
                }

                var monitoringCourseAverageRows = await _db.Calificaciones
                    .AsNoTracking()
                    .Where(x =>
                        !x.Archivado &&
                        x.Curso.Estado == EstadoCurso.Activo &&
                        x.Fecha >= monitoringPeriod.Desde &&
                        x.Fecha <= monitoringPeriod.Hasta &&
                        (
                            x.Tipo == TipoCalificacion.Homework ||
                            x.Tipo == TipoCalificacion.Quiz ||
                            x.Tipo == TipoCalificacion.Test ||
                            x.Tipo == TipoCalificacion.Participation ||
                            x.Tipo == TipoCalificacion.Behaviour
                        ))
                    .GroupBy(x => new { x.CursoId, x.Curso.Nombre, x.Curso.Descripcion })
                    .Select(g => new
                    {
                        g.Key.CursoId,
                        CursoNombre = g.Key.Nombre,
                        CursoDescripcion = g.Key.Descripcion,
                        AverageGrade = Math.Round(g.Average(x => x.Nota), 2)
                    })
                    .Where(x => x.AverageGrade < 75m)
                    .ToListAsync(ct);

                foreach (var row in monitoringCourseAverageRows)
                {
                    if (currentCourseRiskIds.Contains(row.CursoId))
                        continue;

                    var key = $"course-{row.CursoId}-{monitoringPeriod.Anio}-{monitoringPeriod.Trimestre}";

                    if (!openFollowUpsByKey.TryGetValue(key, out var item))
                    {
                        item = new DashboardOpenFollowUpModel
                        {
                            Id = key,
                            EntityType = "course",
                            EntityId = row.CursoId,
                            CursoId = row.CursoId,
                            CursoNombre = row.CursoNombre,
                            CursoDescripcion = row.CursoDescripcion,
                            PeriodLabel = monitoringPeriod.Etiqueta,
                            QuarterNumber = monitoringPeriod.Trimestre,
                            Year = monitoringPeriod.Anio,
                            Source = "course-low-average",
                            Level = row.AverageGrade < 60m ? CourseHealthLevels.Critical : CourseHealthLevels.FollowUp,
                            Href = $"/admin/dashboard/courses/{row.CursoId}/profile"
                        };
                        openFollowUpsByKey[key] = item;
                    }

                    item.AverageGrade = row.AverageGrade;
                    item.Level = row.AverageGrade < 60m ? CourseHealthLevels.Critical : item.Level;
                    item.Source = item.AttendancePercentage.HasValue
                        ? "course-recurring-risk"
                        : "course-low-average";
                    item.Reason = item.AttendancePercentage.HasValue
                        ? $"Promedio grupal {row.AverageGrade:0.##} y asistencia {item.AttendancePercentage:0.##}% en {monitoringPeriod.Etiqueta}"
                        : $"Promedio grupal {row.AverageGrade:0.##} en {monitoringPeriod.Etiqueta}";
                }

                foreach (var row in monitoringClassCountByCourse)
                {
                    if (currentCourseRiskIds.Contains(row.Key))
                        continue;

                    var studentsInCourse = studentCountByCourse.GetValueOrDefault(row.Key);
                    var expectedRecords = row.Value.Count * studentsInCourse;

                    if (expectedRecords == 0)
                        continue;

                    var present = monitoringAttendances.Count(x =>
                        x.CursoId == row.Key &&
                        x.Estado == EstadoAsistencia.Presente);
                    var attendancePercentage = Math.Round((decimal)present * 100 / expectedRecords, 2);

                    if (attendancePercentage >= 85m)
                        continue;

                    var key = $"course-{row.Key}-{monitoringPeriod.Anio}-{monitoringPeriod.Trimestre}";

                    if (!openFollowUpsByKey.TryGetValue(key, out var item))
                    {
                        item = new DashboardOpenFollowUpModel
                        {
                            Id = key,
                            EntityType = "course",
                            EntityId = row.Key,
                            CursoId = row.Key,
                            CursoNombre = row.Value.CursoNombre,
                            CursoDescripcion = row.Value.CursoDescripcion,
                            PeriodLabel = monitoringPeriod.Etiqueta,
                            QuarterNumber = monitoringPeriod.Trimestre,
                            Year = monitoringPeriod.Anio,
                            Source = "course-low-attendance",
                            Level = attendancePercentage < 70m ? CourseHealthLevels.Critical : CourseHealthLevels.FollowUp,
                            Href = $"/admin/dashboard/courses/{row.Key}/profile"
                        };
                        openFollowUpsByKey[key] = item;
                    }

                    item.AttendancePercentage = attendancePercentage;
                    item.Level = attendancePercentage < 70m ? CourseHealthLevels.Critical : item.Level;
                    item.Source = item.AverageGrade.HasValue
                        ? "course-recurring-risk"
                        : "course-low-attendance";
                    item.Reason = item.AverageGrade.HasValue
                        ? $"Promedio grupal {item.AverageGrade:0.##} y asistencia {attendancePercentage:0.##}% en {monitoringPeriod.Etiqueta}"
                        : $"Baja asistencia grupal {attendancePercentage:0.##}% en {monitoringPeriod.Etiqueta}";
                }
            }

            foreach (var pending in coursesPendingFollowUp)
            {
                if (currentCourseRiskIds.Contains(pending.CursoId))
                    continue;

                var key = $"course-{pending.CursoId}-{pending.Year}-{pending.QuarterNumber}";

                if (!openFollowUpsByKey.TryGetValue(key, out var item))
                {
                    openFollowUpsByKey[key] = new DashboardOpenFollowUpModel
                    {
                        Id = key,
                        EntityType = "course",
                        EntityId = pending.CursoId,
                        CursoId = pending.CursoId,
                        CursoNombre = pending.CursoNombre,
                        CursoDescripcion = pending.CursoDescripcion,
                        PeriodLabel = pending.PeriodLabel,
                        QuarterNumber = pending.QuarterNumber,
                        Year = pending.Year,
                        Reason = pending.Reason,
                        Source = "course-pending-follow-up",
                        Level = pending.Level,
                        AverageGrade = pending.AverageGrade,
                        AttendancePercentage = pending.AttendancePercentage,
                        Href = $"/admin/dashboard/courses/{pending.CursoId}/profile"
                    };
                }
            }

            var openFollowUps = openFollowUpsByKey.Values
                .Where(x => !string.IsNullOrWhiteSpace(x.Reason))
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.QuarterNumber)
                .ThenByDescending(x => x.Level == CourseHealthLevels.Critical)
                .ThenBy(GetOpenFollowUpPriority)
                .ThenBy(x => x.AverageGrade ?? 100m)
                .ThenBy(x => x.AttendancePercentage ?? 100m)
                .Take(12)
                .ToList();

            var academicTrends = new List<DashboardAcademicTrendModel>
            {
                new()
                {
                    Key = "average-grade",
                    Label = "Promedio academico",
                    CurrentValue = currentPeriodAverage,
                    PreviousValue = previousPeriodAverage,
                    Delta = currentPeriodAverage.HasValue && previousPeriodAverage.HasValue
                        ? Math.Round(currentPeriodAverage.Value - previousPeriodAverage.Value, 2)
                        : null
                },
                new()
                {
                    Key = "attendance",
                    Label = "Asistencia institucional",
                    CurrentValue = institutionalAttendanceAverage,
                    PreviousValue = previousPeriodAttendanceAverage,
                    Delta = institutionalAttendanceAverage.HasValue && previousPeriodAttendanceAverage.HasValue
                        ? Math.Round(institutionalAttendanceAverage.Value - previousPeriodAttendanceAverage.Value, 2)
                        : null
                }
            };

            // -------------------------------------------------
            // UPCOMING ASSIGNMENTS
            // -------------------------------------------------
            var upcomingAssignments = await _db.Tareas
                .AsNoTracking()
                .Where(x =>
                    x.Estado == EstadoTarea.Publicada &&
                    x.FechaEntregaUtc.HasValue &&
                    x.FechaEntregaUtc.Value >= nowUtc &&
                    x.Curso.Estado == EstadoCurso.Activo)
                .OrderBy(x => x.FechaEntregaUtc)
                .ThenBy(x => x.Titulo)
                .Take(5)
                .Select(x => new DashboardUpcomingAssignmentModel
                {
                    TareaId = x.Id,
                    Titulo = x.Titulo,
                    CursoId = x.CursoId,
                    CursoNombre = x.Curso.Nombre,
                    CursoDescripcion = x.Curso.Descripcion,
                    FechaEntregaUtc = x.FechaEntregaUtc!.Value
                })
                .ToListAsync(ct);

            // -------------------------------------------------
            // UPCOMING CLASSES
            // -------------------------------------------------
            var clasesBase = await _db.CursoHorarios
                .AsNoTracking()
                .Where(x => x.Curso.Estado == EstadoCurso.Activo)
                .Select(x => new
                {
                    x.CursoId,
                    CursoNombre = x.Curso.Nombre,
                    CursoDescripcion = x.Curso.Descripcion,
                    x.Dia,
                    x.HoraInicio,
                    ProfesorNombre = x.Curso.Profesores
                        .Select(cp => cp.Profesor.Usuario.Nombre + " " + cp.Profesor.Usuario.Apellido)
                        .FirstOrDefault()
                })
                .ToListAsync(ct);

            var upcomingClasses = clasesBase
                .Select(x => new DashboardUpcomingClassModel
                {
                    CursoId = x.CursoId,
                    CursoNombre = x.CursoNombre,
                    CursoDescripcion = x.CursoDescripcion,
                    ProfesorNombre = x.ProfesorNombre ?? "Sin asignar",
                    DiaSemana = GetDayName(x.Dia),
                    HoraInicio = x.HoraInicio,
                    ProximaClase = GetNextOccurrence(x.Dia, x.HoraInicio, nowLocal)
                })
                .OrderBy(x => x.ProximaClase)
                .Take(5)
                .ToList();

            var response = new GetAdminDashboardResponseModel
            {
                Period = period,
                TrendComparison = trendComparison,
                ConsecutiveAbsencesWindow = consecutiveAbsencesWindow,
                Overview = overview,
                GeneralAverage = generalAverage,
                CurrentPeriodAverage = currentPeriodAverage,
                InstitutionalAttendanceAverage = institutionalAttendanceAverage,
                InstitutionalHomeworkPendingCorrectionCount = institutionalHomeworkPendingCorrectionCount,
                AverageGradesByCourse = averageGradesByCourse,
                ManualAverageGradesByCourse = manualAverageGradesByCourse,
                StudentsAtRiskThisMonthCount = studentsAtRiskThisMonthCount,
                StudentsManualLowGradesThisMonthCount = studentsManualLowGradesThisMonthCount,
                StudentsManualLowPerformance = studentsManualLowPerformance,
                StudentsAtRiskByAverage = studentsAtRiskByAverage,
                StudentsWithMultipleAbsences = studentsWithMultipleAbsences,
                StudentsWithConsecutiveAbsences = studentsWithConsecutiveAbsences,
                StudentsWithCombinedAcademicRisk = studentsWithCombinedAcademicRisk,
                CoursesAtRiskByAttendance = coursesAtRiskByAttendance,
                CoursesWithAttendanceDecline = coursesWithAttendanceDecline,
                CoursesWithPerformanceDecline = coursesWithPerformanceDecline,
                CriticalCourses = criticalCourses,
                CoursesCurrentRisk = criticalCourses,
                CoursesPendingFollowUp = coursesPendingFollowUp,
                OpenFollowUps = openFollowUps,
                CourseTrendAlerts = courseTrendAlerts,
                AcademicTrends = academicTrends,
                CoursesAtRiskByOverallAverage = coursesAtRiskByOverallAverage,
                CoursesAtRiskByManualAverage = coursesAtRiskByManualAverage,
                UpcomingAssignments = upcomingAssignments,
                UpcomingClasses = upcomingClasses
            };

            return ResponseApiService.Response(StatusCodes.Status200OK, response);
        }

        private static int GetOpenFollowUpPriority(DashboardOpenFollowUpModel item)
        {
            if (item.EntityType == "student" && item.Source == "combined-academic-risk")
                return 0;

            if (item.EntityType == "student" && item.Source == "low-attendance")
                return 1;

            if (item.EntityType == "student" && item.GradeAlerts.Any(x => x.Nota < 50m))
                return 2;

            if (item.EntityType == "course")
                return 3;

            return 4;
        }

        private static string GetTipoCalificacionLabel(TipoCalificacion tipo)
        {
            return tipo switch
            {
                TipoCalificacion.Quiz => "Quiz",
                TipoCalificacion.Test => "Test",
                TipoCalificacion.Participation => "Participación",
                TipoCalificacion.Behaviour => "Comportamiento",
                TipoCalificacion.Homework => "Homework",
                _ => "Evaluación"
            };
        }

        private async Task<decimal?> GetAverageGradeAsync(
            DateOnly from,
            DateOnly to,
            bool includeHomework,
            CancellationToken ct)
        {
            var query = _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    !x.Archivado &&
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Fecha >= from &&
                    x.Fecha <= to &&
                    (
                        x.Tipo == TipoCalificacion.Quiz ||
                        x.Tipo == TipoCalificacion.Test ||
                        x.Tipo == TipoCalificacion.Participation ||
                        x.Tipo == TipoCalificacion.Behaviour ||
                        (includeHomework && x.Tipo == TipoCalificacion.Homework)
                    ));

            var average = await query.Select(x => (decimal?)x.Nota).AverageAsync(ct);

            return average.HasValue ? Math.Round(average.Value, 2) : null;
        }

        private async Task<decimal?> GetInstitutionalAttendanceAverageAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken ct)
        {
            var classes = await _db.Clases
                .AsNoTracking()
                .Where(x =>
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha >= from &&
                    x.Fecha <= to)
                .Select(x => new
                {
                    x.Id,
                    x.CursoId
                })
                .ToListAsync(ct);

            var classIds = classes.Select(x => x.Id).ToList();

            if (classIds.Count == 0)
                return null;

            var studentsByCourse = await _db.Matriculas
                .AsNoTracking()
                .Where(x => x.Curso.Estado == EstadoCurso.Activo)
                .GroupBy(x => x.CursoId)
                .Select(g => new
                {
                    CursoId = g.Key,
                    Count = g.Select(x => x.AlumnoId).Distinct().Count()
                })
                .ToListAsync(ct);

            var studentsByCourseDict = studentsByCourse.ToDictionary(x => x.CursoId, x => x.Count);

            var expectedRecords = classes.Sum(x => studentsByCourseDict.GetValueOrDefault(x.CursoId));

            if (expectedRecords == 0)
                return null;

            var presentes = await _db.Asistencias
                .AsNoTracking()
                .CountAsync(x =>
                    classIds.Contains(x.ClaseId) &&
                    x.Estado == EstadoAsistencia.Presente,
                    ct);

            return Math.Round((decimal)presentes * 100 / expectedRecords, 2);
        }

        private static DateTime GetNextOccurrence(DayOfWeek dia, TimeOnly horaInicio, DateTime fromLocal)
        {
            var currentDate = fromLocal.Date;
            var currentDay = currentDate.DayOfWeek;

            var daysUntil = ((int)dia - (int)currentDay + 7) % 7;

            var nextDate = currentDate.AddDays(daysUntil);
            var nextDateTime = nextDate.Add(horaInicio.ToTimeSpan());

            if (nextDateTime <= fromLocal)
                nextDateTime = nextDateTime.AddDays(7);

            return nextDateTime;
        }

        private static string GetDayName(DayOfWeek dia)
        {
            return dia switch
            {
                DayOfWeek.Monday => "Lunes",
                DayOfWeek.Tuesday => "Martes",
                DayOfWeek.Wednesday => "Miércoles",
                DayOfWeek.Thursday => "Jueves",
                DayOfWeek.Friday => "Viernes",
                DayOfWeek.Saturday => "Sábado",
                DayOfWeek.Sunday => "Domingo",
                _ => "Desconocido"
            };
        }

    }
}
