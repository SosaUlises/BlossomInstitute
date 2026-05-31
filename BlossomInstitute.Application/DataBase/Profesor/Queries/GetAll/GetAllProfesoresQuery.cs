using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Profesor.Queries.GetAllProfesores
{
    public class GetAllProfesoresQuery : IGetAllProfesoresQuery
    {
        private const decimal CourseRiskAverageThreshold = 60m;
        private const decimal CourseRiskAttendanceThreshold = 70m;

        private readonly IDataBaseService _db;

        public GetAllProfesoresQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int pageNumber, int pageSize, string? search)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            search = search?.Trim();

            var rolProfesorId = await _db.Roles
                .Where(r => r.Name == "Profesor")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (rolProfesorId == 0)
                return ResponseApiService.Response(StatusCodes.Status500InternalServerError, message: "Rol Profesor no existe");

            var query = from u in _db.Usuarios.AsNoTracking()
                        join ur in _db.UserRoles.AsNoTracking() on u.Id equals ur.UserId
                        where ur.RoleId == rolProfesorId
                        select u;

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
                .Select(u => new GetProfesorModel
                {
                    Id = u.Id,
                    Email = u.Email!,
                    Nombre = u.Nombre!,
                    Apellido = u.Apellido!,
                    Dni = u.Dni,
                    Telefono = u.PhoneNumber ?? "",
                    AvatarUrl = u.AvatarUrl,
                    Activo = u.Activo
                })
                .ToListAsync();

            if (data.Count > 0)
            {
                await EnrichAcademicContext(data);
            }

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                pageNumber,
                pageSize,
                total,
                items = data
            });
        }

        private async Task EnrichAcademicContext(List<GetProfesorModel> teachers)
        {
            var teacherIds = teachers.Select(x => x.Id).ToList();
            var teacherById = teachers.ToDictionary(x => x.Id);
            var today = GetArgentinaToday();
            var week = GetWeekRange(today);

            var teacherCourseRows = await _db.CursoProfesores
                .AsNoTracking()
                .Where(x => teacherIds.Contains(x.ProfesorId))
                .Select(x => new
                {
                    x.ProfesorId,
                    Course = new GetProfesorCourseModel
                    {
                        Id = x.CursoId,
                        Name = x.Curso.Nombre,
                        Description = x.Curso.Descripcion
                    }
                })
                .ToListAsync();

            foreach (var group in teacherCourseRows.GroupBy(x => x.ProfesorId))
            {
                if (!teacherById.TryGetValue(group.Key, out var teacher)) continue;

                teacher.AssignedCourses = group
                    .Select(x => x.Course)
                    .GroupBy(x => x.Id)
                    .Select(x => x.First())
                    .OrderBy(x => x.Name)
                    .ToList();

                teacher.AssignedCoursesCount = teacher.AssignedCourses.Count;
            }

            var courseIds = teacherCourseRows
                .Select(x => x.Course.Id)
                .Distinct()
                .ToList();

            if (courseIds.Count == 0)
            {
                ApplyFollowUpSignals(teachers);
                return;
            }

            var studentsByTeacher = await (from cp in _db.CursoProfesores.AsNoTracking()
                                           join m in _db.Matriculas.AsNoTracking() on cp.CursoId equals m.CursoId
                                           where teacherIds.Contains(cp.ProfesorId)
                                           select new { cp.ProfesorId, m.AlumnoId })
                .Distinct()
                .GroupBy(x => x.ProfesorId)
                .Select(g => new { ProfesorId = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var item in studentsByTeacher)
            {
                if (teacherById.TryGetValue(item.ProfesorId, out var teacher))
                    teacher.StudentsCount = item.Count;
            }

            var pendingCorrectionsByTeacher = await (from tarea in _db.Tareas.AsNoTracking()
                                                     join entrega in _db.Entregas.AsNoTracking() on tarea.Id equals entrega.TareaId
                                                     where teacherIds.Contains(tarea.ProfesorId) &&
                                                           !_db.EntregaFeedbacks.Any(f => f.EntregaId == entrega.Id && f.EsVigente)
                                                     group entrega by tarea.ProfesorId into g
                                                     select new { ProfesorId = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var item in pendingCorrectionsByTeacher)
            {
                if (teacherById.TryGetValue(item.ProfesorId, out var teacher))
                    teacher.PendingCorrectionsCount = item.Count;
            }

            var classStatsByTeacher = await (from cp in _db.CursoProfesores.AsNoTracking()
                                             join clase in _db.Clases.AsNoTracking() on cp.CursoId equals clase.CursoId
                                             where teacherIds.Contains(cp.ProfesorId) &&
                                                   clase.Estado != EstadoClase.Cancelada &&
                                                   clase.Fecha >= week.Start &&
                                                   clase.Fecha <= week.End
                                             group clase by cp.ProfesorId into g
                                             select new
                                             {
                                                 ProfesorId = g.Key,
                                                 ClassesThisWeek = g.Count(),
                                                 UnloadedAttendanceCount = g.Count(x => x.Fecha <= today && !x.Asistencias.Any())
                                             })
                .ToListAsync();

            foreach (var item in classStatsByTeacher)
            {
                if (!teacherById.TryGetValue(item.ProfesorId, out var teacher)) continue;

                teacher.ClassesThisWeek = item.ClassesThisWeek;
                teacher.UnloadedAttendanceCount = item.UnloadedAttendanceCount;
            }

            var courseAverageRiskIds = await _db.Calificaciones
                .AsNoTracking()
                .Where(x => courseIds.Contains(x.CursoId) && !x.Archivado)
                .GroupBy(x => x.CursoId)
                .Select(g => new { CursoId = g.Key, Average = g.Average(x => x.Nota) })
                .Where(x => x.Average < CourseRiskAverageThreshold)
                .Select(x => x.CursoId)
                .ToListAsync();

            var courseAttendanceRiskIds = await GetCourseAttendanceRiskIds(courseIds, today);
            var courseRiskIds = courseAverageRiskIds
                .Concat(courseAttendanceRiskIds)
                .Distinct()
                .ToHashSet();

            if (courseRiskIds.Count > 0)
            {
                foreach (var group in teacherCourseRows
                             .Where(x => courseRiskIds.Contains(x.Course.Id))
                             .GroupBy(x => x.ProfesorId))
                {
                    if (teacherById.TryGetValue(group.Key, out var teacher))
                        teacher.CoursesAtRiskCount = group.Select(x => x.Course.Id).Distinct().Count();
                }
            }

            ApplyFollowUpSignals(teachers);
        }

        private async Task<HashSet<int>> GetCourseAttendanceRiskIds(List<int> courseIds, DateOnly today)
        {
            var studentCountByCourse = await _db.Matriculas
                .AsNoTracking()
                .Where(x => courseIds.Contains(x.CursoId))
                .GroupBy(x => x.CursoId)
                .Select(g => new { CursoId = g.Key, Count = g.Select(x => x.AlumnoId).Distinct().Count() })
                .ToDictionaryAsync(x => x.CursoId, x => x.Count);

            var classCountByCourse = await _db.Clases
                .AsNoTracking()
                .Where(x =>
                    courseIds.Contains(x.CursoId) &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha <= today)
                .GroupBy(x => x.CursoId)
                .Select(g => new { CursoId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CursoId, x => x.Count);

            var presentCountByCourse = await _db.Asistencias
                .AsNoTracking()
                .Where(x =>
                    courseIds.Contains(x.Clase.CursoId) &&
                    x.Clase.Fecha <= today &&
                    x.Estado == EstadoAsistencia.Presente)
                .GroupBy(x => x.Clase.CursoId)
                .Select(g => new { CursoId = g.Key, PresentCount = g.Count() })
                .ToDictionaryAsync(x => x.CursoId, x => x.PresentCount);

            return classCountByCourse
                .Where(x =>
                {
                    var studentsCount = studentCountByCourse.GetValueOrDefault(x.Key);
                    var expectedRecords = x.Value * studentsCount;
                    if (expectedRecords <= 0) return false;

                    var presentCount = presentCountByCourse.GetValueOrDefault(x.Key);
                    var attendancePercentage = (decimal)presentCount * 100 / expectedRecords;

                    return attendancePercentage < CourseRiskAttendanceThreshold;
                })
                .Select(x => x.Key)
                .ToHashSet();
        }

        private static void ApplyFollowUpSignals(List<GetProfesorModel> teachers)
        {
            foreach (var teacher in teachers)
            {
                teacher.RequiresFollowUp =
                    teacher.AssignedCoursesCount == 0 ||
                    teacher.PendingCorrectionsCount > 0 ||
                    teacher.CoursesAtRiskCount > 0 ||
                    teacher.UnloadedAttendanceCount > 0;

                teacher.MainSignal = BuildMainSignal(teacher);
            }
        }

        private static string BuildMainSignal(GetProfesorModel teacher)
        {
            if (teacher.AssignedCoursesCount == 0)
                return "Sin cursos asignados";

            if (teacher.PendingCorrectionsCount > 0)
                return teacher.PendingCorrectionsCount == 1
                    ? "1 corrección pendiente"
                    : $"{teacher.PendingCorrectionsCount} correcciones pendientes";

            if (teacher.CoursesAtRiskCount > 0)
                return teacher.CoursesAtRiskCount == 1
                    ? "1 curso requiere atención"
                    : $"{teacher.CoursesAtRiskCount} cursos requieren atención";

            if (teacher.UnloadedAttendanceCount > 0)
                return "Asistencias pendientes";

            return "Sin señales pendientes";
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
    }
}
