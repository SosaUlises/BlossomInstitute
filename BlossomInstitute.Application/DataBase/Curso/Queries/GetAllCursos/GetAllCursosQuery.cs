using BlossomInstitute.Common.Features;
using BlossomInstitute.Application.Common.Academico;
using BlossomInstitute.Application.DataBase.Curso.Shared;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Curso.Queries.GetAllCursos
{
    public class GetAllCursosQuery : IGetAllCursosQuery
    {
        private readonly IDataBaseService _db;

        public GetAllCursosQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(
            int pageNumber,
            int pageSize,
            string? search,
            int? anio,
            int? estado)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var query = _db.Cursos
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                query = query.Where(c => c.Nombre.ToLower().Contains(s));
            }

            if (anio.HasValue)
            {
                query = query.Where(c => c.Anio == anio.Value);
            }

            if (estado.HasValue)
            {
                if (estado.Value < 1 || estado.Value > 3)
                    return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Estado inválido");

                query = query.Where(c => (int)c.Estado == estado.Value);
            }

            var total = await query.CountAsync();
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

            var data = await query
                .OrderByDescending(c => c.Anio)
                .ThenBy(c => c.Nombre)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new GetAllCursosModel
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion,
                    Anio = c.Anio,
                    Estado = c.Estado,
                    CantidadHorarios = c.Horarios.Count,
                    CantidadProfesores = c.Profesores.Count,
                    CantidadAlumnos = c.Matriculas.Count,
                    StudentsCount = c.Matriculas.Count,
                    Teachers = c.Profesores
                        .OrderBy(x => x.Profesor.Usuario.Apellido)
                        .ThenBy(x => x.Profesor.Usuario.Nombre)
                        .Select(x => new GetAllCursosTeacherModel
                        {
                            Id = x.ProfesorId,
                            FirstName = x.Profesor.Usuario.Nombre,
                            LastName = x.Profesor.Usuario.Apellido,
                            AvatarUrl = x.Profesor.Usuario.AvatarUrl
                        })
                        .ToList()
                })
                .ToListAsync();

            var courseIds = data.Select(x => x.Id).ToList();

            if (courseIds.Count > 0)
            {
                var academicAverages = await _db.Calificaciones
                    .AsNoTracking()
                    .Where(x =>
                        courseIds.Contains(x.CursoId) &&
                        !x.Archivado &&
                        x.Fecha >= currentFrom &&
                        x.Fecha <= currentTo)
                    .GroupBy(x => x.CursoId)
                    .Select(g => new
                    {
                        CursoId = g.Key,
                        Average = Math.Round(g.Average(x => x.Nota), 2)
                    })
                    .ToDictionaryAsync(x => x.CursoId, x => (decimal?)x.Average);

                var currentClasses = await _db.Clases
                    .AsNoTracking()
                    .Where(x =>
                        courseIds.Contains(x.CursoId) &&
                        x.Estado != EstadoClase.Cancelada &&
                        x.Fecha >= currentFrom &&
                        x.Fecha <= currentTo)
                    .Select(x => new CourseClassProjection
                    {
                        CourseId = x.CursoId,
                        ClassId = x.Id,
                        Date = x.Fecha
                    })
                    .ToListAsync();

                var currentClassIds = currentClasses.Select(x => x.ClassId).ToList();
                var currentClassCountByCourse = currentClasses
                    .GroupBy(x => x.CourseId)
                    .ToDictionary(x => x.Key, x => x.Count());

                var currentAttendanceRows = currentClassIds.Count == 0
                    ? new List<CourseAttendanceProjection>()
                    : await _db.Asistencias
                        .AsNoTracking()
                        .Where(x => currentClassIds.Contains(x.ClaseId))
                        .Select(x => new CourseAttendanceProjection
                        {
                            CourseId = x.Clase.CursoId,
                            ClassId = x.ClaseId,
                            StudentId = x.AlumnoId,
                            Status = x.Estado,
                            Date = x.Clase.Fecha
                        })
                        .ToListAsync();

                var presentByCourse = currentAttendanceRows
                    .GroupBy(x => x.CourseId)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Count(a => a.Status == EstadoAsistencia.Presente));

                var pendingCorrections = await _db.Entregas
                    .AsNoTracking()
                    .Where(x =>
                        courseIds.Contains(x.Tarea.CursoId) &&
                        !x.Feedbacks.Any(f => f.EsVigente))
                    .GroupBy(x => x.Tarea.CursoId)
                    .Select(g => new
                    {
                        CursoId = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(x => x.CursoId, x => x.Count);

                var currentStudentGradeAverages = await _db.Calificaciones
                    .AsNoTracking()
                    .Where(x =>
                        courseIds.Contains(x.CursoId) &&
                        !x.Archivado &&
                        x.Fecha >= currentFrom &&
                        x.Fecha <= currentTo)
                    .GroupBy(x => new { x.CursoId, x.AlumnoId })
                    .Select(g => new
                    {
                        g.Key.CursoId,
                        g.Key.AlumnoId,
                        Average = Math.Round(g.Average(x => x.Nota), 2)
                    })
                    .ToListAsync();

                var studentsAtRiskByAverage = currentStudentGradeAverages
                    .Where(x => x.Average < 60)
                    .Select(x => new { x.CursoId, x.AlumnoId })
                    .ToList();

                var currentStudentAttendance = currentAttendanceRows
                    .GroupBy(x => new { x.CourseId, x.StudentId })
                    .Select(g =>
                    {
                        var classCount = currentClassCountByCourse.GetValueOrDefault(g.Key.CourseId);
                        var attendance = classCount > 0
                            ? (decimal?)Math.Round(g.Count(x => x.Status == EstadoAsistencia.Presente) * 100m / classCount, 2)
                            : null;

                        return new
                        {
                            CursoId = g.Key.CourseId,
                            AlumnoId = g.Key.StudentId,
                            Attendance = attendance
                        };
                    })
                    .ToList();

                var studentsAtRiskByAttendance = currentStudentAttendance
                    .Where(x => x.Attendance.HasValue && x.Attendance.Value < 70)
                    .Select(x => new { x.CursoId, x.AlumnoId })
                    .ToList();

                var studentsAtRisk = studentsAtRiskByAverage
                    .Concat(studentsAtRiskByAttendance)
                    .GroupBy(x => x.CursoId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.AlumnoId).Distinct().Count());

                var lowAttendanceStudents = studentsAtRiskByAttendance
                    .GroupBy(x => x.CursoId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.AlumnoId).Distinct().Count());

                var courseNames = data.ToDictionary(x => x.Id, x => x.Nombre);
                var pendingFollowUp = await BuildPendingFollowUp(courseIds, courseNames, currentFrom);
                var pendingFollowUpByCourse = pendingFollowUp
                    .GroupBy(x => x.CursoId)
                    .ToDictionary(x => x.Key, x => x.ToList());

                foreach (var course in data)
                {
                    course.TeacherNames = course.Teachers
                        .Select(x => $"{x.FirstName} {x.LastName}".Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();
                    course.AvatarUrls = course.Teachers
                        .Select(x => x.AvatarUrl)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Cast<string>()
                        .ToList();

                    var classCount = currentClassCountByCourse.GetValueOrDefault(course.Id);
                    var expectedAttendanceRecords = classCount * course.StudentsCount;
                    var attendanceAverage = expectedAttendanceRecords > 0
                        ? (decimal?)Math.Round(presentByCourse.GetValueOrDefault(course.Id) * 100m / expectedAttendanceRecords, 2)
                        : null;
                    var academicAverage = academicAverages.GetValueOrDefault(course.Id);
                    var currentRiskCount = studentsAtRisk.GetValueOrDefault(course.Id);
                    var coursePendingFollowUp = pendingFollowUpByCourse.GetValueOrDefault(course.Id) ?? new List<CoursePendingFollowUpModel>();

                    course.AcademicAverage = academicAverage;
                    course.AttendanceAverage = attendanceAverage;
                    course.PromedioActual = academicAverage;
                    course.AsistenciaActual = attendanceAverage;
                    course.PendingCorrectionsCount = pendingCorrections.GetValueOrDefault(course.Id);
                    course.StudentsAtRiskCount = currentRiskCount;
                    course.StudentsAtRiskCurrentCount = currentRiskCount;
                    course.AlumnosCriticosActualesCount = currentRiskCount;
                    course.AlumnosConBajaAsistenciaActualCount = lowAttendanceStudents.GetValueOrDefault(course.Id);
                    course.PendingFollowUp = coursePendingFollowUp;
                    course.PendingFollowUpCount = coursePendingFollowUp.Count;
                    course.Period = period;
                    course.MetricsCurrent = new CourseMetricsCurrentModel
                    {
                        AttendanceAverage = attendanceAverage,
                        AcademicAverage = academicAverage,
                        AsistenciaActual = attendanceAverage,
                        PromedioActual = academicAverage,
                        StudentsAtRiskCurrentCount = currentRiskCount,
                        AlumnosCriticosActualesCount = currentRiskCount,
                        AlumnosConBajaAsistenciaActualCount = course.AlumnosConBajaAsistenciaActualCount,
                        PendingFollowUpCount = course.PendingFollowUpCount,
                        PendingCorrectionsCount = course.PendingCorrectionsCount
                    };
                    var health = CourseHealthCalculator.Calculate(
                        course.AttendanceAverage,
                        course.AcademicAverage,
                        course.StudentsAtRiskCount,
                        course.CantidadProfesores > 0);
                    course.HealthStatus = health;
                    course.AcademicStatusCurrent = health;
                    course.MainSignal = GetMainSignal(course);
                    course.RequiresAttention =
                        course.HealthStatus.Level != "normal" ||
                        course.PendingFollowUpCount > 0 ||
                        course.StudentsCount < 5 ||
                        course.PendingCorrectionsCount > 0;
                }
            }

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                pageNumber,
                pageSize,
                total,
                period,
                items = data
            });
        }

        private async Task<List<CoursePendingFollowUpModel>> BuildPendingFollowUp(
            List<int> courseIds,
            Dictionary<int, string> courseNames,
            DateOnly currentFrom)
        {
            var historicalClasses = await _db.Clases
                .AsNoTracking()
                .Where(x =>
                    courseIds.Contains(x.CursoId) &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha < currentFrom)
                .Select(x => new CourseClassProjection
                {
                    CourseId = x.CursoId,
                    ClassId = x.Id,
                    Date = x.Fecha
                })
                .ToListAsync();

            var historicalClassIds = historicalClasses.Select(x => x.ClassId).ToList();
            var historicalAttendanceRows = historicalClassIds.Count == 0
                ? new List<CourseAttendanceProjection>()
                : await _db.Asistencias
                    .AsNoTracking()
                    .Where(x => historicalClassIds.Contains(x.ClaseId))
                    .Select(x => new CourseAttendanceProjection
                    {
                        CourseId = x.Clase.CursoId,
                        ClassId = x.ClaseId,
                        StudentId = x.AlumnoId,
                        StudentFirstName = x.Alumno.Usuario.Nombre,
                        StudentLastName = x.Alumno.Usuario.Apellido,
                        AvatarUrl = x.Alumno.Usuario.AvatarUrl,
                        Status = x.Estado,
                        Date = x.Clase.Fecha
                    })
                    .ToListAsync();

            var historicalGrades = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    courseIds.Contains(x.CursoId) &&
                    !x.Archivado &&
                    x.Fecha < currentFrom)
                .Select(x => new CourseGradeProjection
                {
                    CourseId = x.CursoId,
                    StudentId = x.AlumnoId,
                    StudentFirstName = x.Alumno.Usuario.Nombre,
                    StudentLastName = x.Alumno.Usuario.Apellido,
                    AvatarUrl = x.Alumno.Usuario.AvatarUrl,
                    Grade = x.Nota,
                    Date = x.Fecha
                })
                .ToListAsync();

            var classCountByPeriod = historicalClasses
                .GroupBy(x =>
                {
                    var period = PeriodoAcademicoHelper.ObtenerActual(x.Date);
                    return new CoursePeriodKey(x.CourseId, period.Anio, period.Trimestre);
                })
                .ToDictionary(x => x.Key, x => x.Count());

            var accumulators = new Dictionary<PendingFollowUpKey, PendingFollowUpAccumulator>();

            foreach (var group in historicalGrades.GroupBy(x =>
            {
                var period = PeriodoAcademicoHelper.ObtenerActual(x.Date);
                return new PendingFollowUpKey(x.CourseId, x.StudentId, period.Anio, period.Trimestre);
            }))
            {
                var average = Math.Round(group.Average(x => x.Grade), 2);
                if (average >= 75m)
                    continue;

                var first = group.First();
                var accumulator = GetOrCreatePendingFollowUp(accumulators, group.Key, courseNames, first);
                accumulator.AverageValue = average;
                accumulator.Reasons.Add(average < 60m ? "Bajo rendimiento" : "Rendimiento en seguimiento");
            }

            foreach (var group in historicalAttendanceRows.GroupBy(x =>
            {
                var period = PeriodoAcademicoHelper.ObtenerActual(x.Date);
                return new PendingFollowUpKey(x.CourseId, x.StudentId, period.Anio, period.Trimestre);
            }))
            {
                var classCountKey = new CoursePeriodKey(group.Key.CourseId, group.Key.Year, group.Key.QuarterNumber);
                var classCount = classCountByPeriod.GetValueOrDefault(classCountKey);
                if (classCount <= 0)
                    continue;

                var attendance = Math.Round(group.Count(x => x.Status == EstadoAsistencia.Presente) * 100m / classCount, 2);
                if (attendance >= 85m)
                    continue;

                var first = group.First();
                var accumulator = GetOrCreatePendingFollowUp(accumulators, group.Key, courseNames, first);
                accumulator.AttendanceValue = attendance;
                accumulator.Reasons.Add(attendance < 70m ? "Baja asistencia" : "Asistencia en seguimiento");
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
            Dictionary<int, string> courseNames,
            CourseStudentSignalProjection signal)
        {
            if (accumulators.TryGetValue(key, out var accumulator))
                return accumulator;

            var period = PeriodoAcademicoHelper.ObtenerActual(signal.Date);
            accumulator = new PendingFollowUpAccumulator
            {
                StudentId = signal.StudentId,
                StudentFirstName = signal.StudentFirstName,
                StudentLastName = signal.StudentLastName,
                AvatarUrl = signal.AvatarUrl,
                CourseId = signal.CourseId,
                CourseName = courseNames.GetValueOrDefault(signal.CourseId) ?? string.Empty,
                PeriodLabel = period.Etiqueta,
                QuarterNumber = period.Trimestre,
                Year = period.Anio
            };
            accumulators[key] = accumulator;

            return accumulator;
        }

        private static CourseHealthModel GetHealthStatus(
            decimal? attendanceAverage,
            decimal? academicAverage)
        {
            if (
                (attendanceAverage.HasValue && attendanceAverage.Value < 70) ||
                (academicAverage.HasValue && academicAverage.Value < 60))
            {
                return new CourseHealthModel
                {
                    Level = "critical",
                    Label = "Crítico"
                };
            }

            if (
                (attendanceAverage.HasValue && attendanceAverage.Value < 85) ||
                (academicAverage.HasValue && academicAverage.Value < 75))
            {
                return new CourseHealthModel
                {
                    Level = "follow-up",
                    Label = "Seguimiento"
                };
            }

            return new CourseHealthModel
            {
                Level = "normal",
                Label = "Normal"
            };
        }

        private static string GetMainSignal(GetAllCursosModel course)
        {
            if (course.AttendanceAverage.HasValue && course.AttendanceAverage.Value < 70)
                return "Baja asistencia";

            if (course.AcademicAverage.HasValue && course.AcademicAverage.Value < 60)
                return "Bajo rendimiento";

            if (course.CantidadProfesores == 0)
                return "Sin docentes asignados";

            if (course.StudentsAtRiskCount > 0)
            {
                var label = course.StudentsAtRiskCount == 1 ? "alumno requiere" : "alumnos requieren";
                return $"{course.StudentsAtRiskCount} {label} seguimiento";
            }

            if (course.PendingFollowUpCount > 0)
            {
                var label = course.PendingFollowUpCount == 1 ? "seguimiento pendiente" : "seguimientos pendientes";
                return $"{course.PendingFollowUpCount} {label}";
            }

            if (course.AttendanceAverage.HasValue && course.AttendanceAverage.Value < 85)
                return "Asistencia en seguimiento";

            if (course.AcademicAverage.HasValue && course.AcademicAverage.Value < 75)
                return "Rendimiento en seguimiento";

            if (course.StudentsCount < 5)
                return "Baja matrícula";

            if (course.PendingCorrectionsCount > 0)
            {
                var label = course.PendingCorrectionsCount == 1 ? "corrección pendiente" : "correcciones pendientes";
                return $"{course.PendingCorrectionsCount} {label}";
            }

            return "Sin señales académicas";
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

        private sealed class CourseClassProjection
        {
            public int CourseId { get; init; }
            public int ClassId { get; init; }
            public DateOnly Date { get; init; }
        }

        private abstract class CourseStudentSignalProjection
        {
            public int CourseId { get; init; }
            public int StudentId { get; init; }
            public string StudentFirstName { get; init; } = default!;
            public string StudentLastName { get; init; } = default!;
            public string? AvatarUrl { get; init; }
            public DateOnly Date { get; init; }
        }

        private sealed class CourseAttendanceProjection : CourseStudentSignalProjection
        {
            public int ClassId { get; init; }
            public EstadoAsistencia Status { get; init; }
        }

        private sealed class CourseGradeProjection : CourseStudentSignalProjection
        {
            public decimal Grade { get; init; }
        }

        private readonly record struct CoursePeriodKey(int CourseId, int Year, int QuarterNumber);

        private readonly record struct PendingFollowUpKey(
            int CourseId,
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
                    (AverageValue.HasValue && AverageValue.Value < 60m) ||
                    (AttendanceValue.HasValue && AttendanceValue.Value < 70m);
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
                    Description = $"{(isCritical ? "Crítico" : "Seguimiento")} en {PeriodLabel} {Year}: {reason}"
                };
            }
        }
    }
}
