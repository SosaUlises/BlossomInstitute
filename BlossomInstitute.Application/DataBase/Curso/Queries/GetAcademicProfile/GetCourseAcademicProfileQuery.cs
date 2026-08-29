using BlossomInstitute.Common.Features;
using BlossomInstitute.Application.Common.Academico;
using BlossomInstitute.Application.DataBase.Curso.Shared;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Curso.Queries.GetAcademicProfile
{
    public class GetCourseAcademicProfileQuery : IGetCourseAcademicProfileQuery
    {
        private const decimal CriticalAttendanceThreshold = 70m;
        private const decimal FollowUpAttendanceThreshold = 85m;
        private const decimal CriticalGradeThreshold = 60m;
        private const decimal FollowUpGradeThreshold = 75m;
        private const int LowEnrollmentThreshold = 5;
        private const decimal DeclineThreshold = 10m;

        private readonly IDataBaseService _db;

        public GetCourseAcademicProfileQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int courseId, CancellationToken ct)
        {
            if (courseId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Curso invalido");

            var course = await _db.Cursos
                .AsNoTracking()
                .Where(x => x.Id == courseId)
                .Select(x => new CourseProjection
                {
                    Id = x.Id,
                    Name = x.Nombre,
                    Description = x.Descripcion,
                    Status = x.Estado
                })
                .FirstOrDefaultAsync(ct);

            if (course == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Curso no encontrado");

            var today = GetArgentinaToday();
            var periodContext = PeriodoAcademicoHelper.ObtenerContexto(today);
            var currentFrom = periodContext.Desde;
            var currentTo = periodContext.Hasta;
            var period = new CourseAcademicPeriodModel
            {
                Label = periodContext.Etiqueta,
                From = periodContext.Desde,
                To = periodContext.Hasta,
                Year = periodContext.Anio,
                QuarterNumber = periodContext.NumeroTrimestre
            };
            var recentFrom = today.AddDays(-30);
            var previousFrom = today.AddDays(-60);

            var teachers = await _db.CursoProfesores
                .AsNoTracking()
                .Where(x => x.CursoId == courseId)
                .Select(x => new CourseAcademicProfileTeacherModel
                {
                    Id = x.ProfesorId,
                    FullName = (x.Profesor.Usuario.Nombre + " " + x.Profesor.Usuario.Apellido).Trim(),
                    AvatarUrl = x.Profesor.Usuario.AvatarUrl
                })
                .OrderBy(x => x.FullName)
                .ToListAsync(ct);

            var students = await _db.Matriculas
                .AsNoTracking()
                .Where(x => x.CursoId == courseId)
                .Select(x => new StudentProjection
                {
                    Id = x.AlumnoId,
                    FirstName = x.Alumno.Usuario.Nombre,
                    LastName = x.Alumno.Usuario.Apellido,
                    AvatarUrl = x.Alumno.Usuario.AvatarUrl
                })
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .ToListAsync(ct);

            var studentIds = students.Select(x => x.Id).ToList();

            var classes = await _db.Clases
                .AsNoTracking()
                .Where(x =>
                    x.CursoId == courseId &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha >= currentFrom &&
                    x.Fecha <= currentTo)
                .Select(x => new ClassProjection
                {
                    Id = x.Id,
                    Date = x.Fecha
                })
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Id)
                .ToListAsync(ct);

            var classIds = classes.Select(x => x.Id).ToList();
            var attendanceRows = classIds.Count == 0 || studentIds.Count == 0
                ? new List<AttendanceProjection>()
                : await _db.Asistencias
                    .AsNoTracking()
                    .Where(x => classIds.Contains(x.ClaseId) && studentIds.Contains(x.AlumnoId))
                    .Select(x => new AttendanceProjection
                    {
                        ClassId = x.ClaseId,
                        StudentId = x.AlumnoId,
                        Status = x.Estado,
                        Date = x.Clase.Fecha
                    })
                    .ToListAsync(ct);

            var grades = studentIds.Count == 0
                ? new List<GradeProjection>()
                : await _db.Calificaciones
                    .AsNoTracking()
                    .Where(x =>
                        x.CursoId == courseId &&
                        studentIds.Contains(x.AlumnoId) &&
                        !x.Archivado &&
                        x.Fecha >= currentFrom &&
                        x.Fecha <= currentTo)
                    .Select(x => new GradeProjection
                    {
                        StudentId = x.AlumnoId,
                        Grade = x.Nota,
                        Date = x.Fecha
                    })
                    .ToListAsync(ct);

            var historicalClasses = await _db.Clases
                .AsNoTracking()
                .Where(x =>
                    x.CursoId == courseId &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha < currentFrom)
                .Select(x => new ClassProjection
                {
                    Id = x.Id,
                    Date = x.Fecha
                })
                .ToListAsync(ct);

            var historicalClassIds = historicalClasses.Select(x => x.Id).ToList();
            var historicalAttendanceRows = historicalClassIds.Count == 0 || studentIds.Count == 0
                ? new List<AttendanceProjection>()
                : await _db.Asistencias
                    .AsNoTracking()
                    .Where(x => historicalClassIds.Contains(x.ClaseId) && studentIds.Contains(x.AlumnoId))
                    .Select(x => new AttendanceProjection
                    {
                        ClassId = x.ClaseId,
                        StudentId = x.AlumnoId,
                        Status = x.Estado,
                        Date = x.Clase.Fecha
                    })
                    .ToListAsync(ct);

            var historicalGrades = studentIds.Count == 0
                ? new List<GradeProjection>()
                : await _db.Calificaciones
                    .AsNoTracking()
                    .Where(x =>
                        x.CursoId == courseId &&
                        studentIds.Contains(x.AlumnoId) &&
                        !x.Archivado &&
                        x.Fecha < currentFrom)
                    .Select(x => new GradeProjection
                    {
                        StudentId = x.AlumnoId,
                        Grade = x.Nota,
                        Date = x.Fecha
                    })
                    .ToListAsync(ct);

            var pendingCorrectionsCount = await _db.Entregas
                .AsNoTracking()
                .CountAsync(x =>
                    x.Tarea.CursoId == courseId &&
                    !x.Feedbacks.Any(f => f.EsVigente),
                    ct);

            var attendanceAverage = CalculateAttendanceAverage(classes, attendanceRows, students.Count);
            var academicAverage = grades.Count == 0
                ? null
                : (decimal?)Math.Round(grades.Average(x => x.Grade), 2);

            var studentFollowUp = BuildStudentFollowUp(students, classes, attendanceRows, grades);
            var pendingFollowUp = BuildPendingFollowUp(course, students, historicalClasses, historicalAttendanceRows, historicalGrades);
            var studentsAtRiskCount = studentFollowUp.Count(x =>
                (x.AttendancePercentage.HasValue && x.AttendancePercentage.Value < CriticalAttendanceThreshold) ||
                (x.AverageGrade.HasValue && x.AverageGrade.Value < CriticalGradeThreshold));
            var lowAttendanceStudentsCount = studentFollowUp.Count(x =>
                x.AttendancePercentage.HasValue &&
                x.AttendancePercentage.Value < CriticalAttendanceThreshold);

            var recentAttendanceAverage = CalculateAttendanceAverage(
                classes.Where(x => x.Date >= recentFrom).ToList(),
                attendanceRows.Where(x => x.Date >= recentFrom).ToList(),
                students.Count);
            var previousAttendanceAverage = CalculateAttendanceAverage(
                classes.Where(x => x.Date >= previousFrom && x.Date < recentFrom).ToList(),
                attendanceRows.Where(x => x.Date >= previousFrom && x.Date < recentFrom).ToList(),
                students.Count);

            var recentAcademicAverage = CalculateGradeAverage(grades.Where(x => x.Date >= recentFrom).ToList());
            var previousAcademicAverage = CalculateGradeAverage(
                grades.Where(x => x.Date >= previousFrom && x.Date < recentFrom).ToList());

            var attendanceDropped = HasDeclined(previousAttendanceAverage, recentAttendanceAverage);
            var performanceDeclined = HasDeclined(previousAcademicAverage, recentAcademicAverage);
            var health = CourseHealthCalculator.Calculate(
                attendanceAverage,
                academicAverage,
                studentsAtRiskCount,
                teachers.Count > 0);
            var signals = BuildSignals(
                teachers.Count,
                students.Count,
                attendanceAverage,
                academicAverage,
                studentsAtRiskCount,
                pendingCorrectionsCount,
                attendanceDropped,
                performanceDeclined,
                previousAttendanceAverage,
                recentAttendanceAverage,
                previousAcademicAverage,
                recentAcademicAverage);
            var activity = BuildRecentActivity(
                teachers,
                health,
                signals,
                attendanceDropped,
                performanceDeclined,
                today);

            var response = new CourseAcademicProfileResponseModel
            {
                Course = new CourseAcademicProfileCourseModel
                {
                    Id = course.Id,
                    Name = course.Name,
                    Description = course.Description,
                    Status = course.Status.ToString()
                },
                Teachers = teachers,
                Students = new CourseAcademicProfileStudentsModel
                {
                    StudentsCount = students.Count
                },
                Period = period,
                AcademicMetrics = new CourseAcademicProfileMetricsModel
                {
                    AttendanceAverage = attendanceAverage,
                    AcademicAverage = academicAverage,
                    AsistenciaActual = attendanceAverage,
                    PromedioActual = academicAverage,
                    StudentsAtRiskCount = studentsAtRiskCount,
                    StudentsAtRiskCurrentCount = studentsAtRiskCount,
                    AlumnosCriticosActualesCount = studentsAtRiskCount,
                    AlumnosConBajaAsistenciaActualCount = lowAttendanceStudentsCount,
                    PendingFollowUpCount = pendingFollowUp.Count,
                    PendingCorrectionsCount = pendingCorrectionsCount
                },
                MetricsCurrent = new CourseMetricsCurrentModel
                {
                    AttendanceAverage = attendanceAverage,
                    AcademicAverage = academicAverage,
                    AsistenciaActual = attendanceAverage,
                    PromedioActual = academicAverage,
                    StudentsAtRiskCurrentCount = studentsAtRiskCount,
                    AlumnosCriticosActualesCount = studentsAtRiskCount,
                    AlumnosConBajaAsistenciaActualCount = lowAttendanceStudentsCount,
                    PendingFollowUpCount = pendingFollowUp.Count,
                    PendingCorrectionsCount = pendingCorrectionsCount
                },
                Health = health,
                AcademicStatusCurrent = health,
                StudentsAtRiskCurrentCount = studentsAtRiskCount,
                PendingFollowUpCount = pendingFollowUp.Count,
                AffectedStudentsCurrent = studentFollowUp,
                StudentsRequiringFollowUp = studentFollowUp,
                PendingFollowUp = pendingFollowUp,
                AcademicSignals = signals,
                RecentActivity = activity
            };

            return ResponseApiService.Response(StatusCodes.Status200OK, response);
        }

        private static List<CourseAcademicProfileAffectedStudentModel> BuildStudentFollowUp(
            List<StudentProjection> students,
            List<ClassProjection> classes,
            List<AttendanceProjection> attendanceRows,
            List<GradeProjection> grades)
        {
            var classCount = classes.Count;
            var attendanceByStudent = attendanceRows
                .GroupBy(x => x.StudentId)
                .ToDictionary(
                    x => x.Key,
                    x => classCount > 0
                        ? (decimal?)Math.Round(x.Count(a => a.Status == EstadoAsistencia.Presente) * 100m / classCount, 2)
                        : null);

            var gradesByStudent = grades
                .GroupBy(x => x.StudentId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Count() > 0 ? (decimal?)Math.Round(x.Average(g => g.Grade), 2) : null);

            return students
                .Select(student =>
                {
                    attendanceByStudent.TryGetValue(student.Id, out var attendance);
                    gradesByStudent.TryGetValue(student.Id, out var average);

                    return new CourseAcademicProfileAffectedStudentModel
                    {
                        Id = student.Id,
                        FullName = $"{student.FirstName} {student.LastName}".Trim(),
                        AvatarUrl = student.AvatarUrl,
                        AttendancePercentage = attendance,
                        AverageGrade = average,
                        Reason = BuildStudentReason(attendance, average)
                    };
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Reason))
                .OrderByDescending(x =>
                    (x.AttendancePercentage.HasValue && x.AttendancePercentage.Value < CriticalAttendanceThreshold) ||
                    (x.AverageGrade.HasValue && x.AverageGrade.Value < CriticalGradeThreshold))
                .ThenBy(x => x.AttendancePercentage ?? 100m)
                .ThenBy(x => x.AverageGrade ?? 100m)
                .ToList();
        }

        private static List<CoursePendingFollowUpModel> BuildPendingFollowUp(
            CourseProjection course,
            List<StudentProjection> students,
            List<ClassProjection> historicalClasses,
            List<AttendanceProjection> historicalAttendanceRows,
            List<GradeProjection> historicalGrades)
        {
            var studentsById = students.ToDictionary(x => x.Id);
            var classCountByPeriod = historicalClasses
                .GroupBy(x =>
                {
                    var period = PeriodoAcademicoHelper.ObtenerActual(x.Date);
                    return new PendingCoursePeriodKey(period.Anio, period.Trimestre);
                })
                .ToDictionary(x => x.Key, x => x.Count());
            var accumulators = new Dictionary<PendingFollowUpKey, PendingFollowUpAccumulator>();

            foreach (var group in historicalGrades.GroupBy(x =>
            {
                var period = PeriodoAcademicoHelper.ObtenerActual(x.Date);
                return new PendingFollowUpKey(x.StudentId, period.Anio, period.Trimestre);
            }))
            {
                if (!studentsById.TryGetValue(group.Key.StudentId, out var student))
                    continue;

                var average = Math.Round(group.Average(x => x.Grade), 2);
                if (average >= FollowUpGradeThreshold)
                    continue;

                var period = PeriodoAcademicoHelper.ObtenerActual(group.First().Date);
                var accumulator = GetOrCreatePendingFollowUp(accumulators, group.Key, course, student, period);
                accumulator.AverageValue = average;
                accumulator.Reasons.Add(average < CriticalGradeThreshold ? "Bajo rendimiento" : "Rendimiento en seguimiento");
            }

            foreach (var group in historicalAttendanceRows.GroupBy(x =>
            {
                var period = PeriodoAcademicoHelper.ObtenerActual(x.Date);
                return new PendingFollowUpKey(x.StudentId, period.Anio, period.Trimestre);
            }))
            {
                if (!studentsById.TryGetValue(group.Key.StudentId, out var student))
                    continue;

                var classCountKey = new PendingCoursePeriodKey(group.Key.Year, group.Key.QuarterNumber);
                var classCount = classCountByPeriod.GetValueOrDefault(classCountKey);
                if (classCount <= 0)
                    continue;

                var attendance = Math.Round(group.Count(x => x.Status == EstadoAsistencia.Presente) * 100m / classCount, 2);
                if (attendance >= FollowUpAttendanceThreshold)
                    continue;

                var period = PeriodoAcademicoHelper.ObtenerActual(group.First().Date);
                var accumulator = GetOrCreatePendingFollowUp(accumulators, group.Key, course, student, period);
                accumulator.AttendanceValue = attendance;
                accumulator.Reasons.Add(attendance < CriticalAttendanceThreshold ? "Baja asistencia" : "Asistencia en seguimiento");
            }

            return accumulators.Values
                .Select(x => x.ToModel())
                .OrderByDescending(x => x.Level == CourseHealthLevels.Critical)
                .ThenByDescending(x => x.Year)
                .ThenByDescending(x => x.QuarterNumber)
                .ThenBy(x => x.AlumnoApellido)
                .ThenBy(x => x.AlumnoNombre)
                .ToList();
        }

        private static PendingFollowUpAccumulator GetOrCreatePendingFollowUp(
            Dictionary<PendingFollowUpKey, PendingFollowUpAccumulator> accumulators,
            PendingFollowUpKey key,
            CourseProjection course,
            StudentProjection student,
            PeriodoAcademicoTrimestre period)
        {
            if (accumulators.TryGetValue(key, out var accumulator))
                return accumulator;

            accumulator = new PendingFollowUpAccumulator
            {
                StudentId = student.Id,
                StudentFirstName = student.FirstName,
                StudentLastName = student.LastName,
                AvatarUrl = student.AvatarUrl,
                CourseId = course.Id,
                CourseName = course.Name,
                PeriodLabel = period.Etiqueta,
                QuarterNumber = period.Trimestre,
                Year = period.Anio
            };
            accumulators[key] = accumulator;

            return accumulator;
        }

        private static string BuildStudentReason(decimal? attendancePercentage, decimal? averageGrade)
        {
            var reasons = new List<string>();

            if (attendancePercentage.HasValue && attendancePercentage.Value < CriticalAttendanceThreshold)
                reasons.Add("Baja asistencia");
            else if (attendancePercentage.HasValue && attendancePercentage.Value < FollowUpAttendanceThreshold)
                reasons.Add("Asistencia en seguimiento");

            if (averageGrade.HasValue && averageGrade.Value < CriticalGradeThreshold)
                reasons.Add("Bajo rendimiento");
            else if (averageGrade.HasValue && averageGrade.Value < FollowUpGradeThreshold)
                reasons.Add("Rendimiento en seguimiento");

            return string.Join(" y ", reasons);
        }

        private static CourseHealthModel BuildHealth(
            decimal? attendanceAverage,
            decimal? academicAverage)
        {
            var reasons = new List<string>();

            if (attendanceAverage.HasValue && attendanceAverage.Value < CriticalAttendanceThreshold)
                reasons.Add($"Asistencia {attendanceAverage:0.##}%");
            else if (attendanceAverage.HasValue && attendanceAverage.Value < FollowUpAttendanceThreshold)
                reasons.Add($"Asistencia en seguimiento ({attendanceAverage:0.##}%)");

            if (academicAverage.HasValue && academicAverage.Value < CriticalGradeThreshold)
                reasons.Add($"Promedio {academicAverage:0.##}");
            else if (academicAverage.HasValue && academicAverage.Value < FollowUpGradeThreshold)
                reasons.Add($"Promedio en seguimiento ({academicAverage:0.##})");

            if (
                (attendanceAverage.HasValue && attendanceAverage.Value < CriticalAttendanceThreshold) ||
                (academicAverage.HasValue && academicAverage.Value < CriticalGradeThreshold))
            {
                return new CourseHealthModel
                {
                    Level = "critical",
                    Label = "Critico",
                    Reasons = reasons
                };
            }

            if (
                (attendanceAverage.HasValue && attendanceAverage.Value < FollowUpAttendanceThreshold) ||
                (academicAverage.HasValue && academicAverage.Value < FollowUpGradeThreshold))
            {
                return new CourseHealthModel
                {
                    Level = "follow-up",
                    Label = "Seguimiento",
                    Reasons = reasons
                };
            }

            return new CourseHealthModel
            {
                Level = "normal",
                Label = "Normal",
                Reasons = reasons.Count == 0 ? new List<string> { "Sin alertas academicas" } : reasons
            };
        }

        private static List<CourseAcademicProfileSignalModel> BuildSignals(
            int teachersCount,
            int studentsCount,
            decimal? attendanceAverage,
            decimal? academicAverage,
            int studentsAtRiskCount,
            int pendingCorrectionsCount,
            bool attendanceDropped,
            bool performanceDeclined,
            decimal? previousAttendanceAverage,
            decimal? recentAttendanceAverage,
            decimal? previousAcademicAverage,
            decimal? recentAcademicAverage)
        {
            var signals = new List<CourseAcademicProfileSignalModel>();

            if (attendanceAverage.HasValue && attendanceAverage.Value < CriticalAttendanceThreshold)
                signals.Add(CreateSignal("low-attendance", "Baja asistencia", $"Asistencia promedio {attendanceAverage:0.##}%", "critical"));
            else if (attendanceAverage.HasValue && attendanceAverage.Value < FollowUpAttendanceThreshold)
                signals.Add(CreateSignal("attendance-follow-up", "Asistencia en seguimiento", $"Asistencia promedio {attendanceAverage:0.##}%", "attention"));

            if (academicAverage.HasValue && academicAverage.Value < CriticalGradeThreshold)
                signals.Add(CreateSignal("low-performance", "Bajo rendimiento", $"Promedio academico {academicAverage:0.##}", "critical"));
            else if (academicAverage.HasValue && academicAverage.Value < FollowUpGradeThreshold)
                signals.Add(CreateSignal("performance-follow-up", "Rendimiento en seguimiento", $"Promedio academico {academicAverage:0.##}", "attention"));

            if (attendanceDropped)
                signals.Add(CreateSignal(
                    "attendance-dropped",
                    "Caida de asistencia",
                    $"Paso de {previousAttendanceAverage:0.##}% a {recentAttendanceAverage:0.##}%",
                    "attention"));

            if (performanceDeclined)
                signals.Add(CreateSignal(
                    "performance-declined",
                    "Caida de rendimiento",
                    $"Paso de {previousAcademicAverage:0.##} a {recentAcademicAverage:0.##}",
                    "attention"));

            if (teachersCount == 0)
                signals.Add(CreateSignal("no-teachers", "Sin docentes asignados", "El curso no tiene equipo docente asignado", "critical"));

            if (studentsCount < LowEnrollmentThreshold)
                signals.Add(CreateSignal("low-enrollment", "Baja matricula", $"{studentsCount} alumnos matriculados", "attention"));

            if (studentsAtRiskCount > 0)
            {
                var label = studentsAtRiskCount == 1 ? "1 alumno en riesgo" : $"{studentsAtRiskCount} alumnos en riesgo";
                signals.Add(CreateSignal("students-at-risk", "Alumnos en riesgo", label, "critical"));
            }

            if (pendingCorrectionsCount > 0)
            {
                var label = pendingCorrectionsCount == 1
                    ? "1 correccion pendiente"
                    : $"{pendingCorrectionsCount} correcciones pendientes";
                signals.Add(CreateSignal("pending-corrections", "Correcciones pendientes", label, "neutral"));
            }

            return signals
                .OrderByDescending(x => x.Severity == "critical")
                .ThenByDescending(x => x.Severity == "attention")
                .ThenBy(x => x.Title)
                .ToList();
        }

        private static List<CourseAcademicProfileActivityModel> BuildRecentActivity(
            List<CourseAcademicProfileTeacherModel> teachers,
            CourseHealthModel health,
            List<CourseAcademicProfileSignalModel> signals,
            bool attendanceDropped,
            bool performanceDeclined,
            DateOnly today)
        {
            var activities = new List<CourseAcademicProfileActivityModel>();
            var occurredAt = today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            if (health.Level != "normal")
            {
                activities.Add(new CourseAcademicProfileActivityModel
                {
                    Type = "course-entered-risk-state",
                    Title = health.Level == "critical" ? "Curso en estado critico" : "Curso en seguimiento",
                    Description = string.Join(", ", health.Reasons),
                    Severity = health.Level == "critical" ? "critical" : "attention",
                    OccurredAtUtc = occurredAt
                });
            }

            if (attendanceDropped)
            {
                activities.Add(new CourseAcademicProfileActivityModel
                {
                    Type = "attendance-dropped",
                    Title = "Asistencia en descenso",
                    Description = "La asistencia reciente cayo frente al periodo anterior",
                    Severity = "attention",
                    OccurredAtUtc = occurredAt
                });
            }

            if (performanceDeclined)
            {
                activities.Add(new CourseAcademicProfileActivityModel
                {
                    Type = "performance-declined",
                    Title = "Rendimiento en descenso",
                    Description = "El promedio reciente cayo frente al periodo anterior",
                    Severity = "attention",
                    OccurredAtUtc = occurredAt
                });
            }

            var lowEnrollment = signals.FirstOrDefault(x => x.Type == "low-enrollment");
            if (lowEnrollment != null)
            {
                activities.Add(new CourseAcademicProfileActivityModel
                {
                    Type = "low-enrollment",
                    Title = "Baja matricula",
                    Description = lowEnrollment.Description,
                    Severity = "attention",
                    OccurredAtUtc = occurredAt
                });
            }

            var studentsAtRisk = signals.FirstOrDefault(x => x.Type == "students-at-risk");
            if (studentsAtRisk != null)
            {
                activities.Add(new CourseAcademicProfileActivityModel
                {
                    Type = "students-at-risk",
                    Title = "Alumnos requieren seguimiento",
                    Description = studentsAtRisk.Description,
                    Severity = "critical",
                    OccurredAtUtc = occurredAt
                });
            }

            if (teachers.Count > 0)
            {
                activities.Add(new CourseAcademicProfileActivityModel
                {
                    Type = "teacher-assigned",
                    Title = "Equipo docente asignado",
                    Description = string.Join(", ", teachers.Take(2).Select(x => x.FullName)) +
                        (teachers.Count > 2 ? $" y {teachers.Count - 2} mas" : string.Empty),
                    Severity = "neutral"
                });
            }

            return activities
                .OrderByDescending(x => x.Severity == "critical")
                .ThenByDescending(x => x.Severity == "attention")
                .ThenByDescending(x => x.OccurredAtUtc)
                .Take(8)
                .ToList();
        }

        private static CourseAcademicProfileSignalModel CreateSignal(
            string type,
            string title,
            string description,
            string severity)
        {
            return new CourseAcademicProfileSignalModel
            {
                Type = type,
                Title = title,
                Description = description,
                Severity = severity
            };
        }

        private static decimal? CalculateAttendanceAverage(
            List<ClassProjection> classes,
            List<AttendanceProjection> attendanceRows,
            int studentsCount)
        {
            var expectedRecords = classes.Count * studentsCount;
            if (expectedRecords <= 0) return null;

            return Math.Round(attendanceRows.Count(x => x.Status == EstadoAsistencia.Presente) * 100m / expectedRecords, 2);
        }

        private static decimal? CalculateGradeAverage(List<GradeProjection> grades)
        {
            return grades.Count == 0
                ? null
                : (decimal?)Math.Round(grades.Average(x => x.Grade), 2);
        }

        private static bool HasDeclined(decimal? previous, decimal? recent)
        {
            return previous.HasValue &&
                recent.HasValue &&
                previous.Value - recent.Value >= DeclineThreshold;
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

        private sealed class CourseProjection
        {
            public int Id { get; init; }
            public string Name { get; init; } = default!;
            public string? Description { get; init; }
            public EstadoCurso Status { get; init; }
        }

        private sealed class StudentProjection
        {
            public int Id { get; init; }
            public string FirstName { get; init; } = default!;
            public string LastName { get; init; } = default!;
            public string? AvatarUrl { get; init; }
        }

        private sealed class ClassProjection
        {
            public int Id { get; init; }
            public DateOnly Date { get; init; }
        }

        private sealed class AttendanceProjection
        {
            public int ClassId { get; init; }
            public int StudentId { get; init; }
            public EstadoAsistencia Status { get; init; }
            public DateOnly Date { get; init; }
        }

        private sealed class GradeProjection
        {
            public int StudentId { get; init; }
            public decimal Grade { get; init; }
            public DateOnly Date { get; init; }
        }

        private readonly record struct PendingCoursePeriodKey(int Year, int QuarterNumber);

        private readonly record struct PendingFollowUpKey(
            int StudentId,
            int Year,
            int QuarterNumber);

        private sealed class PendingFollowUpAccumulator
        {
            public int StudentId { get; init; }
            public string StudentFirstName { get; init; } = default!;
            public string StudentLastName { get; init; } = default!;
            public string? AvatarUrl { get; init; }
            public int CourseId { get; init; }
            public string CourseName { get; init; } = default!;
            public string PeriodLabel { get; init; } = default!;
            public int QuarterNumber { get; init; }
            public int Year { get; init; }
            public decimal? AverageValue { get; set; }
            public decimal? AttendanceValue { get; set; }
            public List<string> Reasons { get; } = new();

            public CoursePendingFollowUpModel ToModel()
            {
                var isCritical =
                    (AverageValue.HasValue && AverageValue.Value < CriticalGradeThreshold) ||
                    (AttendanceValue.HasValue && AttendanceValue.Value < CriticalAttendanceThreshold);
                var level = isCritical ? CourseHealthLevels.Critical : CourseHealthLevels.FollowUp;
                var reason = string.Join(" y ", Reasons.Distinct());

                return new CoursePendingFollowUpModel
                {
                    AlumnoId = StudentId,
                    AlumnoNombre = StudentFirstName,
                    AlumnoApellido = StudentLastName,
                    AvatarUrl = AvatarUrl,
                    CursoId = CourseId,
                    CursoNombre = CourseName,
                    PeriodLabel = PeriodLabel,
                    QuarterNumber = QuarterNumber,
                    Year = Year,
                    Level = level,
                    Reason = reason,
                    AverageValue = AverageValue,
                    AttendanceValue = AttendanceValue,
                    Description = $"{(isCritical ? "Critico" : "Seguimiento")} en {PeriodLabel} {Year}: {reason}"
                };
            }
        }
    }
}
