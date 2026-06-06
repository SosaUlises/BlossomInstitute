using BlossomInstitute.Common.Features;
using BlossomInstitute.Application.DataBase.Profesor;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Entidades.Tarea;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Profesor.Queries.GetAcademicSummary
{
    public class GetProfesorAcademicSummaryQuery : IGetProfesorAcademicSummaryQuery
    {
        private const decimal CourseRiskAverageThreshold = 60m;
        private const decimal CourseRiskAttendanceThreshold = 70m;
        private const int RecentActivityLimit = 60;

        private readonly IDataBaseService _db;

        public GetProfesorAcademicSummaryQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int teacherId, CancellationToken ct)
        {
            if (teacherId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Profesor inválido");

            var teacher = await _db.Profesores
                .AsNoTracking()
                .Where(x => x.Id == teacherId)
                .Select(x => new ProfesorAcademicIdentityModel
                {
                    Id = x.Id,
                    AvatarUrl = x.Usuario.AvatarUrl,
                    FirstName = x.Usuario.Nombre,
                    LastName = x.Usuario.Apellido,
                    Email = x.Usuario.Email,
                    Active = x.Usuario.Activo
                })
                .FirstOrDefaultAsync(ct);

            if (teacher == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Profesor no encontrado");

            var today = GetArgentinaToday();
            var week = GetWeekRange(today);

            var courseRows = await _db.CursoProfesores
                .AsNoTracking()
                .Where(x => x.ProfesorId == teacherId)
                .Select(x => new CourseProjection
                {
                    Id = x.CursoId,
                    Name = x.Curso.Nombre,
                    Description = x.Curso.Descripcion
                })
                .OrderBy(x => x.Name)
                .ToListAsync(ct);

            var courseIds = courseRows.Select(x => x.Id).Distinct().ToList();

            var studentsByCourse = courseIds.Count == 0
                ? new Dictionary<int, int>()
                : await _db.Matriculas
                    .AsNoTracking()
                    .Where(x => courseIds.Contains(x.CursoId))
                    .GroupBy(x => x.CursoId)
                    .Select(g => new
                    {
                        CursoId = g.Key,
                        Count = g.Select(x => x.AlumnoId).Distinct().Count()
                    })
                    .ToDictionaryAsync(x => x.CursoId, x => x.Count, ct);

            var studentsCount = courseIds.Count == 0
                ? 0
                : await _db.Matriculas
                    .AsNoTracking()
                    .Where(x => courseIds.Contains(x.CursoId))
                    .Select(x => x.AlumnoId)
                    .Distinct()
                    .CountAsync(ct);

            var classCountByCourse = courseIds.Count == 0
                ? new Dictionary<int, int>()
                : await _db.Clases
                    .AsNoTracking()
                    .Where(x =>
                        courseIds.Contains(x.CursoId) &&
                        x.Estado != EstadoClase.Cancelada &&
                        x.Fecha <= today)
                    .GroupBy(x => x.CursoId)
                    .Select(g => new { CursoId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.CursoId, x => x.Count, ct);

            var presentCountByCourse = courseIds.Count == 0
                ? new Dictionary<int, int>()
                : await _db.Asistencias
                    .AsNoTracking()
                    .Where(x =>
                        courseIds.Contains(x.Clase.CursoId) &&
                        x.Clase.Estado != EstadoClase.Cancelada &&
                        x.Clase.Fecha <= today &&
                        x.Estado == EstadoAsistencia.Presente)
                    .GroupBy(x => x.Clase.CursoId)
                    .Select(g => new { CursoId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.CursoId, x => x.Count, ct);

            var averageGradeByCourse = courseIds.Count == 0
                ? new Dictionary<int, decimal>()
                : await _db.Calificaciones
                    .AsNoTracking()
                    .Where(x => courseIds.Contains(x.CursoId) && !x.Archivado)
                    .GroupBy(x => x.CursoId)
                    .Select(g => new { CursoId = g.Key, Average = g.Average(x => x.Nota) })
                    .ToDictionaryAsync(x => x.CursoId, x => Math.Round(x.Average, 2), ct);

            var assignedCourses = courseRows
                .Select(course => BuildCourseModel(
                    course,
                    studentsByCourse,
                    classCountByCourse,
                    presentCountByCourse,
                    averageGradeByCourse))
                .ToList();

            var pendingCorrectionsCount = await GetPendingCorrectionsCount(teacherId, ct);
            var classesThisWeek = await GetClassesThisWeekCount(courseIds, week, ct);
            var unloadedAttendanceCount = await GetUnloadedAttendanceCount(courseIds, today, ct);
            var operationalStatus = BuildOperationalStatus(
                studentsCount,
                pendingCorrectionsCount,
                unloadedAttendanceCount,
                assignedCourses.Count(x => x.RequiresAttention));
            var recentActivity = await BuildRecentActivity(teacherId, assignedCourses, today, ct);

            var response = new ProfesorAcademicSummaryResponseModel
            {
                Teacher = teacher,
                AssignedCoursesCount = assignedCourses.Count,
                AssignedCourses = assignedCourses,
                StudentsCount = studentsCount,
                PendingCorrectionsCount = pendingCorrectionsCount,
                UnloadedAttendanceCount = unloadedAttendanceCount,
                ClassesThisWeek = classesThisWeek,
                OperationalStatus = operationalStatus,
                RecentActivity = recentActivity
            };

            return ResponseApiService.Response(StatusCodes.Status200OK, response);
        }

        private static ProfesorAcademicCourseModel BuildCourseModel(
            CourseProjection course,
            Dictionary<int, int> studentsByCourse,
            Dictionary<int, int> classCountByCourse,
            Dictionary<int, int> presentCountByCourse,
            Dictionary<int, decimal> averageGradeByCourse)
        {
            var studentsCount = studentsByCourse.GetValueOrDefault(course.Id);
            var classCount = classCountByCourse.GetValueOrDefault(course.Id);
            var expectedRecords = studentsCount * classCount;
            decimal? attendanceAverage = expectedRecords > 0
                ? Math.Round((decimal)presentCountByCourse.GetValueOrDefault(course.Id) * 100 / expectedRecords, 2)
                : null;
            decimal? averageGrade = averageGradeByCourse.TryGetValue(course.Id, out var grade)
                ? grade
                : null;

            return new ProfesorAcademicCourseModel
            {
                Id = course.Id,
                Name = course.Name,
                Description = course.Description,
                StudentsCount = studentsCount,
                AttendanceAverage = attendanceAverage,
                AverageGrade = averageGrade,
                RequiresAttention =
                    (attendanceAverage.HasValue && attendanceAverage.Value < CourseRiskAttendanceThreshold) ||
                    (averageGrade.HasValue && averageGrade.Value < CourseRiskAverageThreshold)
            };
        }

        private async Task<int> GetPendingCorrectionsCount(int teacherId, CancellationToken ct)
        {
            return await (from tarea in _db.Tareas.AsNoTracking()
                          join entrega in _db.Entregas.AsNoTracking() on tarea.Id equals entrega.TareaId
                          where tarea.ProfesorId == teacherId &&
                                !_db.EntregaFeedbacks.Any(f => f.EntregaId == entrega.Id && f.EsVigente)
                          select entrega.Id)
                .CountAsync(ct);
        }

        private async Task<int> GetClassesThisWeekCount(
            List<int> courseIds,
            (DateOnly Start, DateOnly End) week,
            CancellationToken ct)
        {
            if (courseIds.Count == 0) return 0;

            return await _db.Clases
                .AsNoTracking()
                .CountAsync(x =>
                    courseIds.Contains(x.CursoId) &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha >= week.Start &&
                    x.Fecha <= week.End,
                    ct);
        }

        private async Task<int> GetUnloadedAttendanceCount(
            List<int> courseIds,
            DateOnly today,
            CancellationToken ct)
        {
            if (courseIds.Count == 0) return 0;

            return await _db.Clases
                .AsNoTracking()
                .CountAsync(x =>
                    courseIds.Contains(x.CursoId) &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha <= today &&
                    !x.Asistencias.Any(),
                    ct);
        }

        private static ProfesorOperationalStatusModel BuildOperationalStatus(
            int studentsCount,
            int pendingCorrectionsCount,
            int unloadedAttendanceCount,
            int coursesAtRiskCount)
        {
            var reasons = new List<string>();
            var hasRelevantPendingCorrections =
                TeacherFollowUpPolicy.HasRelevantPendingCorrections(
                    pendingCorrectionsCount,
                    studentsCount);

            if (coursesAtRiskCount > 0)
                reasons.Add(coursesAtRiskCount == 1
                    ? "1 curso requiere atención"
                    : $"{coursesAtRiskCount} cursos requieren atención");

            if (unloadedAttendanceCount > 0)
                reasons.Add(unloadedAttendanceCount == 1
                    ? "1 asistencia pendiente"
                    : $"{unloadedAttendanceCount} asistencias pendientes");

            if (hasRelevantPendingCorrections)
                reasons.Add($"{pendingCorrectionsCount} correcciones acumuladas");

            if (coursesAtRiskCount > 0)
            {
                return new ProfesorOperationalStatusModel
                {
                    Level = "critical",
                    Label = "Crítico",
                    Reasons = reasons
                };
            }

            if (reasons.Count > 0)
            {
                return new ProfesorOperationalStatusModel
                {
                    Level = "follow-up",
                    Label = "Seguimiento",
                    Reasons = reasons
                };
            }

            return new ProfesorOperationalStatusModel
            {
                Level = "normal",
                Label = "Normal",
                Reasons = new List<string> { "Sin señales pendientes" }
            };
        }

        private async Task<List<ProfesorRecentActivityModel>> BuildRecentActivity(
            int teacherId,
            List<ProfesorAcademicCourseModel> assignedCourses,
            DateOnly today,
            CancellationToken ct)
        {
            var courseIds = assignedCourses.Select(x => x.Id).ToList();
            var activities = new List<ProfesorRecentActivityModel>();

            activities.AddRange(BuildCourseAttentionActivity(assignedCourses));

            if (courseIds.Count == 0)
                return activities.Take(RecentActivityLimit).ToList();

            var attendanceRows = await _db.Clases
                .AsNoTracking()
                .Where(x =>
                    courseIds.Contains(x.CursoId) &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha <= today &&
                    x.Asistencias.Any())
                .OrderByDescending(x => x.Fecha)
                .ThenByDescending(x => x.Id)
                .Take(RecentActivityLimit)
                .Select(x => new AttendanceActivityProjection
                {
                    CourseId = x.CursoId,
                    CourseName = x.Curso.Nombre,
                    Date = x.Fecha
                })
                .ToListAsync(ct);

            activities.AddRange(attendanceRows.Select(x => new ProfesorRecentActivityModel
            {
                Type = "attendance-loaded",
                Title = "Asistencia cargada",
                Description = $"Se cargó asistencia en {x.CourseName}",
                Severity = "neutral",
                CourseId = x.CourseId,
                CourseName = x.CourseName,
                OccurredAtUtc = x.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            }));

            var taskRows = await _db.Tareas
                .AsNoTracking()
                .Where(x => x.ProfesorId == teacherId && x.Estado == EstadoTarea.Publicada)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(RecentActivityLimit)
                .Select(x => new TaskActivityProjection
                {
                    CourseId = x.CursoId,
                    CourseName = x.Curso.Nombre,
                    Title = x.Titulo,
                    OccurredAtUtc = x.CreatedAtUtc
                })
                .ToListAsync(ct);

            activities.AddRange(taskRows.Select(x => new ProfesorRecentActivityModel
            {
                Type = "task-published",
                Title = "Tarea publicada",
                Description = x.Title,
                Severity = "neutral",
                CourseId = x.CourseId,
                CourseName = x.CourseName,
                OccurredAtUtc = x.OccurredAtUtc
            }));

            var correctionRows = await _db.EntregaFeedbacks
                .AsNoTracking()
                .Where(x => x.EsVigente && x.Entrega.Tarea.ProfesorId == teacherId)
                .OrderByDescending(x => x.FechaCorreccionUtc)
                .Take(RecentActivityLimit)
                .Select(x => new CorrectionActivityProjection
                {
                    CourseId = x.Entrega.Tarea.CursoId,
                    CourseName = x.Entrega.Tarea.Curso.Nombre,
                    TaskTitle = x.Entrega.Tarea.Titulo,
                    OccurredAtUtc = x.FechaCorreccionUtc
                })
                .ToListAsync(ct);

            activities.AddRange(correctionRows.Select(x => new ProfesorRecentActivityModel
            {
                Type = "correction-completed",
                Title = "Corrección completada",
                Description = $"Se corrigió {x.TaskTitle}",
                Severity = "neutral",
                CourseId = x.CourseId,
                CourseName = x.CourseName,
                OccurredAtUtc = x.OccurredAtUtc
            }));

            return activities
                .OrderByDescending(x => x.Severity == "critical")
                .ThenByDescending(x => x.Severity == "attention")
                .ThenByDescending(x => x.OccurredAtUtc)
                .Take(RecentActivityLimit)
                .ToList();
        }

        private static IEnumerable<ProfesorRecentActivityModel> BuildCourseAttentionActivity(
            List<ProfesorAcademicCourseModel> assignedCourses)
        {
            return assignedCourses
                .Where(x => x.RequiresAttention)
                .OrderBy(x => x.Name)
                .Select(x => new ProfesorRecentActivityModel
                {
                    Type = "course-requires-attention",
                    Title = "Curso requiere atención",
                    Description = BuildCourseAttentionDescription(x),
                    Severity =
                        (x.AttendanceAverage.HasValue && x.AttendanceAverage.Value < 60m) ||
                        (x.AverageGrade.HasValue && x.AverageGrade.Value < 50m)
                            ? "critical"
                            : "attention",
                    CourseId = x.Id,
                    CourseName = x.Name
                });
        }

        private static string BuildCourseAttentionDescription(ProfesorAcademicCourseModel course)
        {
            var reasons = new List<string>();

            if (course.AttendanceAverage.HasValue &&
                course.AttendanceAverage.Value < CourseRiskAttendanceThreshold)
            {
                reasons.Add($"asistencia {course.AttendanceAverage:0.##}%");
            }

            if (course.AverageGrade.HasValue &&
                course.AverageGrade.Value < CourseRiskAverageThreshold)
            {
                reasons.Add($"promedio {course.AverageGrade:0.##}");
            }

            return reasons.Count == 0
                ? "El curso tiene señales pendientes"
                : string.Join(", ", reasons);
        }

        private static DateOnly GetArgentinaToday()
        {
            TimeZoneInfo argentinaTimeZone;

            try
            {
                argentinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
            }
            catch
            {
                argentinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");
            }

            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, argentinaTimeZone));
        }

        private static (DateOnly Start, DateOnly End) GetWeekRange(DateOnly today)
        {
            var daysFromMonday = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            var start = today.AddDays(-daysFromMonday);

            return (start, start.AddDays(6));
        }

        private sealed class CourseProjection
        {
            public int Id { get; init; }
            public string Name { get; init; } = default!;
            public string? Description { get; init; }
        }

        private sealed class AttendanceActivityProjection
        {
            public int CourseId { get; init; }
            public string CourseName { get; init; } = default!;
            public DateOnly Date { get; init; }
        }

        private sealed class TaskActivityProjection
        {
            public int CourseId { get; init; }
            public string CourseName { get; init; } = default!;
            public string Title { get; init; } = default!;
            public DateTime OccurredAtUtc { get; init; }
        }

        private sealed class CorrectionActivityProjection
        {
            public int CourseId { get; init; }
            public string CourseName { get; init; } = default!;
            public string TaskTitle { get; init; } = default!;
            public DateTime OccurredAtUtc { get; init; }
        }
    }
}
