using BlossomInstitute.Common.Features;
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
            var firstDayOfMonth = new DateOnly(today.Year, today.Month, 1);
            var previousMonthStart = firstDayOfMonth.AddMonths(-1);
            var previousMonthEnd = firstDayOfMonth.AddDays(-1);
            var periodFromUtc = firstDayOfMonth.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var periodToUtcExclusive = today.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var period = new DashboardPeriodModel
            {
                Strategy = "current-month",
                From = firstDayOfMonth,
                To = today,
                Year = today.Year,
                Month = today.Month
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

            // -------------------------------------------------
            // -------------------------------------------------
            // CURRENT PERIOD GENERAL AVERAGE (TODOS LOS TIPOS)
            // Strategy: current month. Keeping GeneralAverage populated
            // with this value for backwards compatibility with the frontend.
            // -------------------------------------------------
            var generalAverageRaw = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    !x.Archivado &&
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Fecha >= firstDayOfMonth &&
                    x.Fecha <= today &&
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
                previousMonthStart,
                previousMonthEnd,
                includeHomework: true,
                ct);

            // -------------------------------------------------
            // AVERAGE GRADES BY COURSE (GENERAL, CURRENT MONTH)
            // -------------------------------------------------
            var averageGradesByCourse = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    !x.Archivado &&
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Fecha >= firstDayOfMonth &&
                    x.Fecha <= today &&
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

            // -------------------------------------------------
            // -------------------------------------------------
            // AVERAGE GRADES BY COURSE (SOLO MANUALES, CURRENT MONTH)
            // -------------------------------------------------
            var manualAverageGradesByCourse = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    !x.Archivado &&
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Fecha >= firstDayOfMonth &&
                    x.Fecha <= today &&
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

            // -------------------------------------------------
            // STUDENTS AT RISK THIS MONTH (GENERAL)
            // promedio mensual < 60 incluyendo homework + manuales
            // -------------------------------------------------
            var studentsAtRiskThisMonthCount = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    !x.Archivado &&
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Fecha >= firstDayOfMonth &&
                    x.Fecha <= today &&
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

            var studentsAtRiskByAverage = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    !x.Archivado &&
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Fecha >= firstDayOfMonth &&
                    x.Fecha <= today &&
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
                    x.CursoId,
                    CursoNombre = x.Curso.Nombre,
                    CursoDescripcion = x.Curso.Descripcion
                })
                .Select(g => new DashboardStudentAverageRiskModel
                {
                    AlumnoId = g.Key.AlumnoId,
                    AlumnoNombre = g.Key.AlumnoNombre,
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
            // evaluaciones manuales < 60 en el mes
            // -------------------------------------------------
            var lowManualGradesBaseQuery = _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    !x.Archivado &&
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Fecha >= firstDayOfMonth &&
                    x.Fecha <= today &&
                    x.Nota < 60 &&
                    (
                        x.Tipo == TipoCalificacion.Quiz ||
                        x.Tipo == TipoCalificacion.Test ||
                        x.Tipo == TipoCalificacion.Participation ||
                        x.Tipo == TipoCalificacion.Behaviour
                    ));

            // -------------------------------------------------
            // STUDENTS WITH AT LEAST ONE LOW MANUAL GRADE THIS MONTH
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
            // ATTENDANCE HEALTH (CURRENT MONTH)
            // -------------------------------------------------
            var periodClasses = await _db.Clases
                .AsNoTracking()
                .Where(x =>
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha >= firstDayOfMonth &&
                    x.Fecha <= today)
                .Select(x => new
                {
                    x.Id,
                    x.CursoId,
                    CursoNombre = x.Curso.Nombre,
                    CursoDescripcion = x.Curso.Descripcion
                })
                .ToListAsync(ct);

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
                    AlumnoNombre = x.Alumno.Usuario.Nombre + " " + x.Alumno.Usuario.Apellido
                })
                .ToListAsync(ct);

            var periodAttendances = await _db.Asistencias
                .AsNoTracking()
                .Where(x => periodClassIds.Contains(x.ClaseId))
                .Select(x => new
                {
                    x.AlumnoId,
                    x.Clase.CursoId,
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
                previousMonthStart,
                previousMonthEnd,
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
                .Where(x => x.ExpectedAttendanceRecords > 0 && x.AttendancePercentage < 80)
                .OrderBy(x => x.AttendancePercentage)
                .Take(5)
                .ToList();

            var attendanceByStudentCourse = periodAttendances
                .GroupBy(x => new { x.AlumnoId, x.CursoId })
                .ToDictionary(g => (g.Key.AlumnoId, g.Key.CursoId), g => new
                {
                    Presentes = g.Count(x => x.Estado == EstadoAsistencia.Presente),
                    Ausentes = g.Count(x => x.Estado == EstadoAsistencia.Ausente)
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
                        CursoId = x.CursoId,
                        CursoNombre = x.CursoNombre,
                        CursoDescripcion = x.CursoDescripcion,
                        Ausentes = ausentes,
                        ClasesTotales = classCount,
                        AttendancePercentage = percentage
                    };
                })
                .Where(x => x.ClasesTotales > 0 && x.Ausentes >= 3)
                .OrderByDescending(x => x.Ausentes)
                .ThenBy(x => x.AttendancePercentage)
                .Take(8)
                .ToList();

            // -------------------------------------------------
            // HOMEWORK HEALTH (CURRENT MONTH)
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

            var criticalCourses = criticalCourseIds
                .Select(courseId =>
                {
                    var signals = 0;
                    if (coursesAtRiskByOverallAverage.Any(x => x.CursoId == courseId)) signals++;
                    if (coursesAtRiskByManualAverage.Any(x => x.CursoId == courseId)) signals++;
                    if (coursesAtRiskByAttendance.Any(x => x.CursoId == courseId)) signals++;
                    if (pendingHomeworkByCourseDict.GetValueOrDefault(courseId) >= 5) signals++;

                    return new DashboardCriticalCourseModel
                    {
                        CursoId = courseId,
                        CursoNombre = courseNamesById.GetValueOrDefault(courseId, "Curso"),
                        CursoDescripcion = courseDescriptionsById.GetValueOrDefault(courseId),
                        AverageGrade = averageByCourseDict.GetValueOrDefault(courseId),
                        AttendancePercentage = attendanceRiskByCourseDict.GetValueOrDefault(courseId),
                        PendingCorrectionCount = pendingHomeworkByCourseDict.GetValueOrDefault(courseId),
                        SignalsCount = signals
                    };
                })
                .OrderByDescending(x => x.SignalsCount)
                .ThenBy(x => x.AverageGrade ?? 100)
                .Take(5)
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
                CoursesAtRiskByAttendance = coursesAtRiskByAttendance,
                CriticalCourses = criticalCourses,
                AcademicTrends = academicTrends,
                CoursesAtRiskByOverallAverage = coursesAtRiskByOverallAverage,
                CoursesAtRiskByManualAverage = coursesAtRiskByManualAverage,
                UpcomingAssignments = upcomingAssignments,
                UpcomingClasses = upcomingClasses
            };

            return ResponseApiService.Response(StatusCodes.Status200OK, response);
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
