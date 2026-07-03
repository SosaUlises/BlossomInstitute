using BlossomInstitute.Application.Common.Academico;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Calificacion;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Entidades.Entrega;
using BlossomInstitute.Domain.Entidades.Tarea;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Alumno.Queries.GetAcademicSummary
{
    public class GetAlumnoAcademicSummaryQuery : IGetAlumnoAcademicSummaryQuery
    {
        private const decimal LowAttendanceThreshold = 70m;
        private const decimal LowGradeThreshold = 60m;

        private readonly IDataBaseService _db;

        public GetAlumnoAcademicSummaryQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int studentId, CancellationToken ct)
        {
            if (studentId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Alumno invalido");

            var student = await _db.Alumnos
                .AsNoTracking()
                .Where(x => x.Id == studentId)
                .Select(x => new
                {
                    x.Id,
                    x.Usuario.Nombre,
                    x.Usuario.Apellido,
                    x.Usuario.Email,
                    x.Usuario.Dni,
                    x.Usuario.Activo,
                    x.Usuario.AvatarUrl
                })
                .FirstOrDefaultAsync(ct);

            if (student == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Alumno no encontrado");

            var today = DateOnly.FromDateTime(DateTime.Now);
            var periodContext = PeriodoAcademicoHelper.ObtenerContexto(today);
            var quarter = periodContext.TrimestreActual;
            var dataTo = periodContext.Hasta;
            var fromUtc = quarter.Desde.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var toUtcExclusive = dataTo.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var period = new AlumnoAcademicPeriodModel
            {
                Type = "academic-quarter",
                Label = quarter.Etiqueta,
                MonthRangeLabel = quarter.EtiquetaRangoMeses,
                From = periodContext.Desde,
                To = periodContext.Hasta,
                Year = periodContext.Anio,
                Quarter = periodContext.NumeroTrimestre
            };

            var enrollments = await _db.Matriculas
                .AsNoTracking()
                .Where(x => x.AlumnoId == studentId)
                .Select(x => new EnrollmentProjection
                {
                    CourseId = x.CursoId,
                    CourseName = x.Curso.Nombre,
                    CourseDescription = x.Curso.Descripcion,
                    CourseStatus = x.Curso.Estado
                })
                .OrderBy(x => x.CourseStatus == EstadoCurso.Activo ? 0 : 1)
                .ThenBy(x => x.CourseName)
                .ToListAsync(ct);

            var courseIds = enrollments.Select(x => x.CourseId).Distinct().ToList();
            var teacherRows = courseIds.Count == 0
                ? new List<TeacherProjection>()
                : await _db.CursoProfesores
                    .AsNoTracking()
                    .Where(x => courseIds.Contains(x.CursoId))
                    .Select(x => new TeacherProjection
                    {
                        CourseId = x.CursoId,
                        FirstName = x.Profesor.Usuario.Nombre,
                        LastName = x.Profesor.Usuario.Apellido,
                        AvatarUrl = x.Profesor.Usuario.AvatarUrl
                    })
                    .OrderBy(x => x.LastName)
                    .ThenBy(x => x.FirstName)
                    .ToListAsync(ct);

            var teachersByCourse = teacherRows
                .GroupBy(x => x.CourseId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(t => new TeacherSummaryProjection
                    {
                        Name = $"{t.FirstName} {t.LastName}".Trim(),
                        AvatarUrl = t.AvatarUrl
                    }).First());

            var currentEnrollments = enrollments
                .Where(x => x.CourseStatus == EstadoCurso.Activo)
                .Select((x, index) => ToEnrollmentModel(x, teachersByCourse, index == 0))
                .ToList();

            var currentCourseIds = currentEnrollments.Select(x => x.CourseId).ToList();
            var attendanceSummary = new AlumnoAcademicAttendanceSummaryModel();
            var gradesSummary = new AlumnoAcademicGradesSummaryModel();
            var homeworkSummary = new AlumnoAcademicHomeworkSummaryModel();
            DateOnly? latestAbsenceDate = null;

            if (currentCourseIds.Count > 0)
            {
                var classes = await _db.Clases
                    .AsNoTracking()
                    .Where(x =>
                        currentCourseIds.Contains(x.CursoId) &&
                        x.Estado != EstadoClase.Cancelada &&
                        x.Fecha >= quarter.Desde &&
                        x.Fecha <= dataTo)
                    .Select(x => new ClassProjection
                    {
                        Id = x.Id,
                        CourseId = x.CursoId,
                        Date = x.Fecha
                    })
                    .OrderBy(x => x.Date)
                    .ThenBy(x => x.Id)
                    .ToListAsync(ct);

                var classIds = classes.Select(x => x.Id).ToList();
                var attendanceRows = classIds.Count == 0
                    ? new List<AttendanceProjection>()
                    : await _db.Asistencias
                        .AsNoTracking()
                        .Where(x => x.AlumnoId == studentId && classIds.Contains(x.ClaseId))
                        .Select(x => new AttendanceProjection
                        {
                            ClassId = x.ClaseId,
                            Status = x.Estado,
                            Date = x.Clase.Fecha
                        })
                        .ToListAsync(ct);

                attendanceSummary = BuildAttendanceSummary(classes, attendanceRows, out latestAbsenceDate);

                var grades = await _db.Calificaciones
                    .AsNoTracking()
                    .Where(x =>
                        x.AlumnoId == studentId &&
                        currentCourseIds.Contains(x.CursoId) &&
                        !x.Archivado &&
                        x.Fecha >= quarter.Desde &&
                        x.Fecha <= dataTo &&
                        (
                            x.Tipo == TipoCalificacion.Homework ||
                            x.Tipo == TipoCalificacion.Quiz ||
                            x.Tipo == TipoCalificacion.Test ||
                            x.Tipo == TipoCalificacion.Participation ||
                            x.Tipo == TipoCalificacion.Behaviour
                        ))
                    .Select(x => new GradeProjection
                    {
                        Id = x.Id,
                        CourseId = x.CursoId,
                        CourseName = x.Curso.Nombre,
                        Title = x.Titulo,
                        Type = x.Tipo,
                        Grade = x.Nota,
                        Date = x.Fecha
                    })
                    .ToListAsync(ct);

                gradesSummary = BuildGradesSummary(grades);
                homeworkSummary = await BuildHomeworkSummaryAsync(studentId, currentCourseIds, fromUtc, toUtcExclusive, ct);
            }

            var academicStatus = BuildAcademicStatus(attendanceSummary, gradesSummary);
            var recentSignals = BuildRecentSignals(attendanceSummary, gradesSummary, homeworkSummary, latestAbsenceDate);
            var pendingFollowUp = courseIds.Count == 0
                ? new List<AlumnoAcademicPendingFollowUpModel>()
                : await BuildPendingFollowUpAsync(
                    studentId,
                    courseIds,
                    enrollments.ToDictionary(x => x.CourseId, x => x.CourseName),
                    quarter.Desde,
                    periodContext.TrimestreAnterior,
                    ct);

            var fullName = $"{student.Nombre} {student.Apellido}".Trim();
            var response = new AlumnoAcademicSummaryResponseModel
            {
                Student = new AlumnoAcademicIdentityModel
                {
                    Id = student.Id,
                    FirstName = student.Nombre,
                    LastName = student.Apellido,
                    FullName = fullName,
                    Email = student.Email,
                    Dni = student.Dni,
                    Active = student.Activo,
                    AvatarUrl = student.AvatarUrl
                },
                Period = period,
                CurrentCourse = currentEnrollments.FirstOrDefault(),
                CurrentEnrollments = currentEnrollments,
                AttendanceSummary = attendanceSummary,
                GradesSummary = gradesSummary,
                HomeworkSummary = homeworkSummary,
                AcademicStatus = academicStatus,
                PendingFollowUp = pendingFollowUp,
                RecentSignals = recentSignals
            };

            return ResponseApiService.Response(StatusCodes.Status200OK, response);
        }

        private static AlumnoAcademicEnrollmentModel ToEnrollmentModel(
            EnrollmentProjection enrollment,
            Dictionary<int, TeacherSummaryProjection> teachersByCourse,
            bool isMain)
        {
            teachersByCourse.TryGetValue(enrollment.CourseId, out var teacher);

            return new AlumnoAcademicEnrollmentModel
            {
                CourseId = enrollment.CourseId,
                CourseName = enrollment.CourseName,
                CourseDescription = enrollment.CourseDescription,
                CourseStatus = enrollment.CourseStatus.ToString(),
                TeacherName = teacher?.Name,
                TeacherAvatarUrl = teacher?.AvatarUrl,
                IsMain = isMain
            };
        }

        private async Task<List<AlumnoAcademicPendingFollowUpModel>> BuildPendingFollowUpAsync(
            int studentId,
            List<int> courseIds,
            Dictionary<int, string> courseNames,
            DateOnly currentFrom,
            PeriodoAcademicoTrimestre previousQuarter,
            CancellationToken ct)
        {
            var historicalClasses = await _db.Clases
                .AsNoTracking()
                .Where(x =>
                    courseIds.Contains(x.CursoId) &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha < currentFrom)
                .Select(x => new ClassProjection
                {
                    Id = x.Id,
                    CourseId = x.CursoId,
                    Date = x.Fecha
                })
                .ToListAsync(ct);

            var historicalClassIds = historicalClasses.Select(x => x.Id).ToList();
            var historicalAttendanceRows = historicalClassIds.Count == 0
                ? new List<AttendanceProjection>()
                : await _db.Asistencias
                    .AsNoTracking()
                    .Where(x => x.AlumnoId == studentId && historicalClassIds.Contains(x.ClaseId))
                    .Select(x => new AttendanceProjection
                    {
                        ClassId = x.ClaseId,
                        Date = x.Clase.Fecha,
                        Status = x.Estado
                    })
                    .ToListAsync(ct);

            var historicalGrades = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    x.AlumnoId == studentId &&
                    courseIds.Contains(x.CursoId) &&
                    !x.Archivado &&
                    x.Fecha < currentFrom &&
                    (
                        x.Tipo == TipoCalificacion.Homework ||
                        x.Tipo == TipoCalificacion.Quiz ||
                        x.Tipo == TipoCalificacion.Test ||
                        x.Tipo == TipoCalificacion.Participation ||
                        x.Tipo == TipoCalificacion.Behaviour
                    ))
                .Select(x => new GradeProjection
                {
                    Id = x.Id,
                    CourseId = x.CursoId,
                    CourseName = x.Curso.Nombre,
                    Title = x.Titulo,
                    Type = x.Tipo,
                    Grade = x.Nota,
                    Date = x.Fecha
                })
                .ToListAsync(ct);

            return BuildPendingFollowUp(
                courseNames,
                historicalClasses,
                historicalAttendanceRows,
                historicalGrades,
                previousQuarter);
        }

        private static List<AlumnoAcademicPendingFollowUpModel> BuildPendingFollowUp(
            Dictionary<int, string> courseNames,
            List<ClassProjection> historicalClasses,
            List<AttendanceProjection> historicalAttendanceRows,
            List<GradeProjection> historicalGrades,
            PeriodoAcademicoTrimestre previousQuarter)
        {
            var classById = historicalClasses.ToDictionary(x => x.Id);
            var classCountByPeriod = historicalClasses
                .GroupBy(x =>
                {
                    var period = PeriodoAcademicoHelper.ObtenerActual(x.Date);
                    return new PendingFollowUpKey(x.CourseId, period.Anio, period.Trimestre);
                })
                .ToDictionary(x => x.Key, x => x.Count());
            var accumulators = new Dictionary<PendingFollowUpKey, PendingFollowUpAccumulator>();

            foreach (var group in historicalGrades.GroupBy(x =>
            {
                var period = PeriodoAcademicoHelper.ObtenerActual(x.Date);
                return new PendingFollowUpKey(x.CourseId, period.Anio, period.Trimestre);
            }))
            {
                var average = Math.Round(group.Average(x => x.Grade), 2);
                if (average >= 75m)
                    continue;

                var period = PeriodoAcademicoHelper.ObtenerActual(group.First().Date);
                var courseName = courseNames.GetValueOrDefault(group.Key.CourseId) ?? group.First().CourseName;
                var accumulator = GetOrCreatePendingFollowUp(accumulators, group.Key, courseName, period, previousQuarter);
                accumulator.AverageValue = average;
                accumulator.Reasons.Add(average < LowGradeThreshold ? "Promedio bajo" : "Promedio en seguimiento");
            }

            foreach (var group in historicalAttendanceRows
                .Where(x => classById.ContainsKey(x.ClassId))
                .GroupBy(x =>
                {
                    var cls = classById[x.ClassId];
                    var period = PeriodoAcademicoHelper.ObtenerActual(x.Date);
                    return new PendingFollowUpKey(cls.CourseId, period.Anio, period.Trimestre);
                }))
            {
                var key = group.Key;
                var classCount = classCountByPeriod.GetValueOrDefault(key);
                if (classCount <= 0)
                    continue;

                var attendance = Math.Round(group.Count(x => x.Status == EstadoAsistencia.Presente) * 100m / classCount, 2);
                if (attendance >= 85m)
                    continue;

                var period = PeriodoAcademicoHelper.ObtenerActual(group.First().Date);
                var courseName = courseNames.GetValueOrDefault(key.CourseId) ?? string.Empty;
                var accumulator = GetOrCreatePendingFollowUp(accumulators, key, courseName, period, previousQuarter);
                accumulator.AttendanceValue = attendance;
                accumulator.Reasons.Add(attendance < LowAttendanceThreshold ? "Baja asistencia" : "Asistencia en seguimiento");
            }

            return accumulators.Values
                .Select(x => x.ToModel())
                .OrderByDescending(x => x.Level == "critical")
                .ThenByDescending(x => x.IsPreviousQuarter)
                .ThenByDescending(x => x.Year)
                .ThenByDescending(x => x.QuarterNumber)
                .ThenBy(x => x.CourseName)
                .ToList();
        }

        private static PendingFollowUpAccumulator GetOrCreatePendingFollowUp(
            Dictionary<PendingFollowUpKey, PendingFollowUpAccumulator> accumulators,
            PendingFollowUpKey key,
            string courseName,
            PeriodoAcademicoTrimestre period,
            PeriodoAcademicoTrimestre previousQuarter)
        {
            if (accumulators.TryGetValue(key, out var accumulator))
                return accumulator;

            accumulator = new PendingFollowUpAccumulator
            {
                CourseId = key.CourseId,
                CourseName = courseName,
                PeriodLabel = period.Etiqueta,
                QuarterNumber = period.Trimestre,
                Year = period.Anio,
                IsPreviousQuarter = period.Anio == previousQuarter.Anio &&
                    period.Trimestre == previousQuarter.Trimestre
            };
            accumulators[key] = accumulator;

            return accumulator;
        }

        private static AlumnoAcademicAttendanceSummaryModel BuildAttendanceSummary(
            List<ClassProjection> classes,
            List<AttendanceProjection> attendanceRows,
            out DateOnly? latestAbsenceDate)
        {
            latestAbsenceDate = attendanceRows
                .Where(x => x.Status == EstadoAsistencia.Ausente)
                .OrderByDescending(x => x.Date)
                .Select(x => (DateOnly?)x.Date)
                .FirstOrDefault();

            if (classes.Count == 0)
            {
                return new AlumnoAcademicAttendanceSummaryModel
                {
                    PresentCount = 0,
                    AbsentCount = 0,
                    TotalClasses = 0,
                    ConsecutiveAbsences = 0
                };
            }

            var presentCount = attendanceRows.Count(x => x.Status == EstadoAsistencia.Presente);
            var absentCount = attendanceRows.Count(x => x.Status == EstadoAsistencia.Ausente);
            var percentage = Math.Round((decimal)presentCount * 100 / classes.Count, 2);
            var maxConsecutiveAbsences = GetMaxConsecutiveAbsences(classes, attendanceRows);

            return new AlumnoAcademicAttendanceSummaryModel
            {
                AttendancePercentage = percentage,
                PresentCount = presentCount,
                AbsentCount = absentCount,
                TotalClasses = classes.Count,
                ConsecutiveAbsences = maxConsecutiveAbsences,
                IsLowAttendance = percentage < LowAttendanceThreshold
            };
        }

        private static int GetMaxConsecutiveAbsences(
            List<ClassProjection> classes,
            List<AttendanceProjection> attendanceRows)
        {
            var attendanceByClass = attendanceRows
                .GroupBy(x => x.ClassId)
                .ToDictionary(x => x.Key, x => x.First().Status);

            var current = 0;
            var max = 0;

            foreach (var cls in classes.OrderBy(x => x.Date).ThenBy(x => x.Id))
            {
                if (attendanceByClass.TryGetValue(cls.Id, out var status) && status == EstadoAsistencia.Ausente)
                {
                    current++;
                    max = Math.Max(max, current);
                    continue;
                }

                if (status == EstadoAsistencia.Presente)
                    current = 0;
            }

            return max;
        }

        private static AlumnoAcademicGradesSummaryModel BuildGradesSummary(List<GradeProjection> grades)
        {
            if (grades.Count == 0)
            {
                return new AlumnoAcademicGradesSummaryModel
                {
                    LowGradesCount = 0
                };
            }

            var manualGrades = grades
                .Where(x => x.Type != TipoCalificacion.Homework)
                .ToList();

            var latestLowGrade = grades
                .Where(x => x.Grade < LowGradeThreshold)
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();

            var latestGrade = grades
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .First();

            return new AlumnoAcademicGradesSummaryModel
            {
                AverageGrade = Math.Round(grades.Average(x => x.Grade), 2),
                ManualAverageGrade = manualGrades.Count > 0
                    ? Math.Round(manualGrades.Average(x => x.Grade), 2)
                    : null,
                LowGradesCount = grades.Count(x => x.Grade < LowGradeThreshold),
                LatestLowGrade = latestLowGrade == null ? null : ToGradeSignalModel(latestLowGrade),
                LatestGrade = ToGradeSignalModel(latestGrade)
            };
        }

        private async Task<AlumnoAcademicHomeworkSummaryModel> BuildHomeworkSummaryAsync(
            int studentId,
            List<int> currentCourseIds,
            DateTime fromUtc,
            DateTime toUtcExclusive,
            CancellationToken ct)
        {
            var taskIds = await _db.Tareas
                .AsNoTracking()
                .Where(x =>
                    currentCourseIds.Contains(x.CursoId) &&
                    x.Estado == EstadoTarea.Publicada &&
                    x.FechaEntregaUtc.HasValue &&
                    x.FechaEntregaUtc.Value >= fromUtc &&
                    x.FechaEntregaUtc.Value < toUtcExclusive)
                .Select(x => x.Id)
                .ToListAsync(ct);

            if (taskIds.Count == 0)
            {
                return new AlumnoAcademicHomeworkSummaryModel
                {
                    PendingSubmissions = 0,
                    PendingCorrections = 0,
                    ApprovedCount = 0,
                    NeedsRevisionCount = 0
                };
            }

            var deliveries = await _db.Entregas
                .AsNoTracking()
                .Where(x => x.AlumnoId == studentId && taskIds.Contains(x.TareaId))
                .Select(x => new
                {
                    x.Id,
                    x.TareaId
                })
                .ToListAsync(ct);

            var deliveryIds = deliveries.Select(x => x.Id).ToList();
            var feedbacks = deliveryIds.Count == 0
                ? new List<FeedbackProjection>()
                : await _db.EntregaFeedbacks
                    .AsNoTracking()
                    .Where(x => deliveryIds.Contains(x.EntregaId) && x.EsVigente)
                    .Select(x => new FeedbackProjection
                    {
                        DeliveryId = x.EntregaId,
                        Status = x.Estado
                    })
                    .ToListAsync(ct);

            return new AlumnoAcademicHomeworkSummaryModel
            {
                PendingSubmissions = taskIds.Count - deliveries.Select(x => x.TareaId).Distinct().Count(),
                PendingCorrections = deliveries.Count - feedbacks.Select(x => x.DeliveryId).Distinct().Count(),
                ApprovedCount = feedbacks.Count(x => x.Status == EstadoCorreccion.Aprobado),
                NeedsRevisionCount = feedbacks.Count(x => x.Status == EstadoCorreccion.Rehacer)
            };
        }

        private static AlumnoAcademicStatusModel BuildAcademicStatus(
            AlumnoAcademicAttendanceSummaryModel attendance,
            AlumnoAcademicGradesSummaryModel grades)
        {
            var attendanceLow = attendance.AttendancePercentage.HasValue &&
                attendance.AttendancePercentage.Value < LowAttendanceThreshold;
            var averageLow = grades.AverageGrade.HasValue &&
                grades.AverageGrade.Value < LowGradeThreshold;
            var consecutiveAbsences = attendance.ConsecutiveAbsences ?? 0;

            var reasons = new List<string>();

            if (attendanceLow)
                reasons.Add($"Asistencia trimestral {attendance.AttendancePercentage:0.##}%");

            if (averageLow)
                reasons.Add($"Promedio trimestral {grades.AverageGrade:0.##}");

            if (consecutiveAbsences >= 2)
                reasons.Add($"{consecutiveAbsences} ausencias consecutivas");

            if ((attendanceLow && averageLow) || consecutiveAbsences >= 2)
            {
                return new AlumnoAcademicStatusModel
                {
                    Level = "critical",
                    Label = "Requiere intervencion prioritaria",
                    Reasons = reasons
                };
            }

            if (attendanceLow || averageLow)
            {
                return new AlumnoAcademicStatusModel
                {
                    Level = "follow-up",
                    Label = "Requiere seguimiento",
                    Reasons = reasons
                };
            }

            return new AlumnoAcademicStatusModel
            {
                Level = "normal",
                Label = "Sin alertas academicas",
                Reasons = reasons
            };
        }

        private static List<AlumnoAcademicSignalModel> BuildRecentSignals(
            AlumnoAcademicAttendanceSummaryModel attendance,
            AlumnoAcademicGradesSummaryModel grades,
            AlumnoAcademicHomeworkSummaryModel homework,
            DateOnly? latestAbsenceDate)
        {
            var signals = new List<AlumnoAcademicSignalModel>();

            if (grades.LatestLowGrade != null)
            {
                signals.Add(new AlumnoAcademicSignalModel
                {
                    Type = "low-grade",
                    Title = "Nota baja",
                    Description = $"{grades.LatestLowGrade.Title}: {grades.LatestLowGrade.Grade:0.##}",
                    Severity = grades.LatestLowGrade.Grade < 50 ? "critical" : "attention",
                    Date = grades.LatestLowGrade.Date
                });
            }

            if (attendance.AttendancePercentage.HasValue &&
                attendance.AttendancePercentage.Value < LowAttendanceThreshold)
            {
                signals.Add(new AlumnoAcademicSignalModel
                {
                    Type = "low-attendance",
                    Title = "Baja asistencia",
                    Description = $"Asistencia trimestral {attendance.AttendancePercentage:0.##}%",
                    Severity = attendance.AttendancePercentage.Value < 60 ? "critical" : "attention",
                    Date = latestAbsenceDate
                });
            }

            if ((attendance.ConsecutiveAbsences ?? 0) >= 2)
            {
                signals.Add(new AlumnoAcademicSignalModel
                {
                    Type = "consecutive-absences",
                    Title = "Ausencias consecutivas",
                    Description = $"{attendance.ConsecutiveAbsences} ausencias consecutivas registradas",
                    Severity = "critical",
                    Date = latestAbsenceDate
                });
            }

            if ((homework.PendingSubmissions ?? 0) > 0)
            {
                signals.Add(new AlumnoAcademicSignalModel
                {
                    Type = "missing-homework",
                    Title = "Entregas pendientes",
                    Description = $"{homework.PendingSubmissions} tarea(s) sin entregar en el trimestre",
                    Severity = "attention"
                });
            }

            return signals
                .OrderByDescending(x => x.Severity == "critical")
                .ThenByDescending(x => x.Date)
                .Take(6)
                .ToList();
        }

        private static AlumnoAcademicGradeSignalModel ToGradeSignalModel(GradeProjection grade)
        {
            return new AlumnoAcademicGradeSignalModel
            {
                Id = grade.Id,
                CourseId = grade.CourseId,
                CourseName = grade.CourseName,
                Title = grade.Title,
                Type = grade.Type.ToString(),
                Grade = Math.Round(grade.Grade, 2),
                Date = grade.Date
            };
        }

        private sealed class EnrollmentProjection
        {
            public int CourseId { get; init; }
            public string CourseName { get; init; } = default!;
            public string? CourseDescription { get; init; }
            public EstadoCurso CourseStatus { get; init; }
        }

        private sealed class TeacherProjection
        {
            public int CourseId { get; init; }
            public string FirstName { get; init; } = default!;
            public string LastName { get; init; } = default!;
            public string? AvatarUrl { get; init; }
        }

        private sealed class TeacherSummaryProjection
        {
            public string Name { get; init; } = default!;
            public string? AvatarUrl { get; init; }
        }

        private sealed class ClassProjection
        {
            public int Id { get; init; }
            public int CourseId { get; init; }
            public DateOnly Date { get; init; }
        }

        private sealed class AttendanceProjection
        {
            public int ClassId { get; init; }
            public DateOnly Date { get; init; }
            public EstadoAsistencia Status { get; init; }
        }

        private sealed class GradeProjection
        {
            public int Id { get; init; }
            public int CourseId { get; init; }
            public string CourseName { get; init; } = default!;
            public string Title { get; init; } = default!;
            public TipoCalificacion Type { get; init; }
            public decimal Grade { get; init; }
            public DateOnly Date { get; init; }
        }

        private sealed class FeedbackProjection
        {
            public int DeliveryId { get; init; }
            public EstadoCorreccion Status { get; init; }
        }

        private readonly record struct PendingFollowUpKey(
            int CourseId,
            int Year,
            int QuarterNumber);

        private sealed class PendingFollowUpAccumulator
        {
            public int CourseId { get; init; }
            public string CourseName { get; init; } = default!;
            public string PeriodLabel { get; init; } = default!;
            public int QuarterNumber { get; init; }
            public int Year { get; init; }
            public decimal? AverageValue { get; set; }
            public decimal? AttendanceValue { get; set; }
            public bool IsPreviousQuarter { get; init; }
            public List<string> Reasons { get; } = new();

            public AlumnoAcademicPendingFollowUpModel ToModel()
            {
                var isCritical =
                    (AverageValue.HasValue && AverageValue.Value < LowGradeThreshold) ||
                    (AttendanceValue.HasValue && AttendanceValue.Value < LowAttendanceThreshold);
                var level = isCritical ? "critical" : "follow-up";
                var reason = string.Join(" y ", Reasons.Distinct());
                var mainReason = Reasons.Distinct().FirstOrDefault() ?? "Seguimiento pendiente";
                var valueDetail = AverageValue.HasValue
                    ? $"Promedio {AverageValue:0.##}"
                    : AttendanceValue.HasValue
                        ? $"Asistencia {AttendanceValue:0.##}%"
                        : mainReason;

                return new AlumnoAcademicPendingFollowUpModel
                {
                    CourseId = CourseId,
                    CourseName = CourseName,
                    PeriodLabel = PeriodLabel,
                    QuarterNumber = QuarterNumber,
                    Year = Year,
                    Level = level,
                    Title = IsPreviousQuarter
                        ? "Seguimiento pendiente del trimestre anterior"
                        : $"{(isCritical ? "Critico" : "Seguimiento")} en {PeriodLabel}",
                    Reason = reason,
                    AverageValue = AverageValue,
                    AttendanceValue = AttendanceValue,
                    Description = $"{valueDetail} en {PeriodLabel}",
                    Status = "pending",
                    IsPreviousQuarter = IsPreviousQuarter
                };
            }
        }
    }
}
