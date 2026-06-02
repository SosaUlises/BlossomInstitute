using BlossomInstitute.Application.Common.Academic;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Alumno.Queries.GetAll
{
    public class GetAllAlumnosQuery : IGetAllAlumnosQuery
    {
        private readonly IDataBaseService _db;

        public GetAllAlumnosQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int pageNumber, int pageSize, string? search)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            search = search?.Trim();

            var rolAlumnoId = await _db.Roles
                .Where(r => r.Name == "Alumno")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (rolAlumnoId == 0)
                return ResponseApiService.Response(StatusCodes.Status500InternalServerError, message: "Rol Alumno no existe");

            // Usuarios que son Alumnos
            var query = from u in _db.Usuarios.AsNoTracking()
                        join ur in _db.UserRoles.AsNoTracking() on u.Id equals ur.UserId
                        where ur.RoleId == rolAlumnoId
                        select u;

            // Search (Nombre/Apellido/Email)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLowerInvariant();
                query = query.Where(u =>
                    (u.Nombre ?? "").ToLower().Contains(s) ||
                    (u.Apellido ?? "").ToLower().Contains(s) ||
                    (u.Email ?? "").ToLower().Contains(s));
            }

            var total = await query.CountAsync();

            var data = await query
                .OrderBy(u => u.Apellido)
                .ThenBy(u => u.Nombre)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new GetAlumnoModel
                {
                    Id = u.Id,
                    Email = u.Email!,
                    Nombre = u.Nombre!,
                    Apellido = u.Apellido!,
                    Dni = u.Dni,
                    Telefono = u.PhoneNumber ?? "",
                    Activo = u.Activo,
                    IsActive = u.Activo,
                    AvatarUrl = u.AvatarUrl
                })
                .ToListAsync();

            await AddAcademicContextAsync(data);

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                pageNumber,
                pageSize,
                total,
                items = data
            });
        }

        private async Task AddAcademicContextAsync(List<GetAlumnoModel> students)
        {
            if (students.Count == 0)
                return;

            var studentIds = students.Select(x => x.Id).ToList();
            var today = DateOnly.FromDateTime(DateTime.Now);
            var periodContext = AcademicQuarterHelper.GetContext(today);
            var quarter = periodContext.CurrentQuarter;
            var dataTo = periodContext.To;

            var enrollments = await _db.Matriculas
                .AsNoTracking()
                .Where(x =>
                    studentIds.Contains(x.AlumnoId) &&
                    x.Curso.Estado == EstadoCurso.Activo)
                .Select(x => new EnrollmentProjection
                {
                    StudentId = x.AlumnoId,
                    CourseId = x.CursoId,
                    CourseName = x.Curso.Nombre,
                    CourseDescription = x.Curso.Descripcion
                })
                .OrderBy(x => x.StudentId)
                .ThenBy(x => x.CourseName)
                .ToListAsync();

            var enrollmentsByStudent = enrollments
                .GroupBy(x => x.StudentId)
                .ToDictionary(x => x.Key, x => x.ToList());

            foreach (var student in students)
            {
                var studentEnrollments = enrollmentsByStudent.GetValueOrDefault(student.Id) ?? new List<EnrollmentProjection>();
                var currentEnrollment = studentEnrollments.FirstOrDefault();

                student.CurrentCourseId = currentEnrollment?.CourseId;
                student.CurrentCourseName = currentEnrollment?.CourseName;
                student.CurrentCourseDescription = currentEnrollment?.CourseDescription;
                student.HasActiveEnrollment = currentEnrollment != null;
                student.IsWithoutCourse = currentEnrollment == null;
            }

            var courseIds = enrollments.Select(x => x.CourseId).Distinct().ToList();
            var classes = courseIds.Count == 0
                ? new List<ClassProjection>()
                : await _db.Clases
                    .AsNoTracking()
                    .Where(x =>
                        courseIds.Contains(x.CursoId) &&
                        x.Estado != EstadoClase.Cancelada &&
                        x.Fecha >= quarter.From &&
                        x.Fecha <= dataTo)
                    .Select(x => new ClassProjection
                    {
                        Id = x.Id,
                        CourseId = x.CursoId,
                        Date = x.Fecha
                    })
                    .OrderBy(x => x.Date)
                    .ThenBy(x => x.Id)
                    .ToListAsync();

            var classIds = classes.Select(x => x.Id).ToList();
            var attendanceRows = classIds.Count == 0
                ? new List<AttendanceProjection>()
                : await _db.Asistencias
                    .AsNoTracking()
                    .Where(x =>
                        studentIds.Contains(x.AlumnoId) &&
                        classIds.Contains(x.ClaseId))
                    .Select(x => new AttendanceProjection
                    {
                        StudentId = x.AlumnoId,
                        ClassId = x.ClaseId,
                        Status = x.Estado
                    })
                    .ToListAsync();

            var grades = courseIds.Count == 0
                ? new List<GradeProjection>()
                : await _db.Calificaciones
                    .AsNoTracking()
                    .Where(x =>
                        studentIds.Contains(x.AlumnoId) &&
                        courseIds.Contains(x.CursoId) &&
                        !x.Archivado &&
                        x.Fecha >= quarter.From &&
                        x.Fecha <= dataTo)
                    .Select(x => new GradeProjection
                    {
                        Id = x.Id,
                        StudentId = x.AlumnoId,
                        CourseId = x.CursoId,
                        CourseName = x.Curso.Nombre,
                        Title = x.Titulo,
                        Grade = x.Nota,
                        Date = x.Fecha
                    })
                    .ToListAsync();

            var classesByCourse = classes
                .GroupBy(x => x.CourseId)
                .ToDictionary(x => x.Key, x => x.ToList());

            var attendanceByStudent = attendanceRows
                .GroupBy(x => x.StudentId)
                .ToDictionary(x => x.Key, x => x.ToList());

            var gradesByStudent = grades
                .GroupBy(x => x.StudentId)
                .ToDictionary(x => x.Key, x => x.ToList());

            foreach (var student in students)
            {
                var studentEnrollments = enrollmentsByStudent.GetValueOrDefault(student.Id) ?? new List<EnrollmentProjection>();
                var studentCourseIds = studentEnrollments.Select(x => x.CourseId).ToHashSet();
                var studentClasses = studentCourseIds
                    .SelectMany(courseId => classesByCourse.GetValueOrDefault(courseId) ?? new List<ClassProjection>())
                    .OrderBy(x => x.Date)
                    .ThenBy(x => x.Id)
                    .ToList();
                var studentAttendance = attendanceByStudent.GetValueOrDefault(student.Id) ?? new List<AttendanceProjection>();

                ApplyAttendance(student, studentClasses, studentAttendance);
                ApplyGrades(student, gradesByStudent.GetValueOrDefault(student.Id) ?? new List<GradeProjection>());
                ApplyAcademicStatus(student);
            }
        }

        private static void ApplyAttendance(
            GetAlumnoModel student,
            List<ClassProjection> classes,
            List<AttendanceProjection> attendanceRows)
        {
            student.ConsecutiveAbsences = 0;

            if (classes.Count == 0)
                return;

            var presentCount = attendanceRows.Count(x => x.Status == EstadoAsistencia.Presente);
            student.AttendancePercentage = Math.Round((decimal)presentCount * 100 / classes.Count, 2);
            student.ConsecutiveAbsences = GetMaxConsecutiveAbsences(classes, attendanceRows);
        }

        private static void ApplyGrades(GetAlumnoModel student, List<GradeProjection> grades)
        {
            if (grades.Count == 0)
                return;

            student.AverageGrade = Math.Round(grades.Average(x => x.Grade), 2);

            var latestLowGrade = grades
                .Where(x => x.Grade < 60)
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();

            if (latestLowGrade == null)
                return;

            student.LatestLowGrade = new GetAlumnoLatestLowGradeModel
            {
                Id = latestLowGrade.Id,
                CourseId = latestLowGrade.CourseId,
                CourseName = latestLowGrade.CourseName,
                Title = latestLowGrade.Title,
                Grade = Math.Round(latestLowGrade.Grade, 2),
                Date = latestLowGrade.Date
            };
        }

        private static void ApplyAcademicStatus(GetAlumnoModel student)
        {
            var attendanceLow = student.AttendancePercentage.HasValue && student.AttendancePercentage.Value < 70;
            var averageLow = student.AverageGrade.HasValue && student.AverageGrade.Value < 60;
            var hasConsecutiveAbsences = (student.ConsecutiveAbsences ?? 0) >= 2;
            var hasLatestLowGrade = student.LatestLowGrade != null;

            var reasons = new List<string>();

            if (student.IsWithoutCourse)
                reasons.Add("Sin curso activo");

            if (attendanceLow)
                reasons.Add($"Asistencia trimestral {student.AttendancePercentage!.Value:0.##}%");

            if (averageLow)
                reasons.Add($"Promedio trimestral {student.AverageGrade!.Value:0.##}");

            if (hasConsecutiveAbsences)
                reasons.Add($"{student.ConsecutiveAbsences} ausencias consecutivas");

            if (hasLatestLowGrade)
                reasons.Add($"Ultima nota baja: {student.LatestLowGrade!.Grade:0.##}");

            student.AcademicReasons = reasons;

            if (hasConsecutiveAbsences || (attendanceLow && averageLow))
            {
                student.AcademicStatusLevel = "critical";
                student.AcademicStatusLabel = "Requiere intervencion prioritaria";
                return;
            }

            if (attendanceLow || averageLow || hasLatestLowGrade || student.IsWithoutCourse)
            {
                student.AcademicStatusLevel = "follow-up";
                student.AcademicStatusLabel = "Requiere seguimiento";
                return;
            }

            student.AcademicStatusLevel = "normal";
            student.AcademicStatusLabel = "Sin alertas academicas";
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

        private sealed class EnrollmentProjection
        {
            public int StudentId { get; init; }
            public int CourseId { get; init; }
            public string CourseName { get; init; } = default!;
            public string? CourseDescription { get; init; }
        }

        private sealed class ClassProjection
        {
            public int Id { get; init; }
            public int CourseId { get; init; }
            public DateOnly Date { get; init; }
        }

        private sealed class AttendanceProjection
        {
            public int StudentId { get; init; }
            public int ClassId { get; init; }
            public EstadoAsistencia Status { get; init; }
        }

        private sealed class GradeProjection
        {
            public int Id { get; init; }
            public int StudentId { get; init; }
            public int CourseId { get; init; }
            public string CourseName { get; init; } = default!;
            public string Title { get; init; } = default!;
            public decimal Grade { get; init; }
            public DateOnly Date { get; init; }
        }
    }
}
