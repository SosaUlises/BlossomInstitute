using BlossomInstitute.Common.Features;
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
                    .Where(x => courseIds.Contains(x.CursoId) && !x.Archivado)
                    .GroupBy(x => x.CursoId)
                    .Select(g => new
                    {
                        CursoId = g.Key,
                        Average = Math.Round(g.Average(x => x.Nota), 2)
                    })
                    .ToDictionaryAsync(x => x.CursoId, x => (decimal?)x.Average);

                var attendanceAverages = await _db.Asistencias
                    .AsNoTracking()
                    .Where(x => courseIds.Contains(x.Clase.CursoId) && x.Clase.Estado != EstadoClase.Cancelada)
                    .GroupBy(x => x.Clase.CursoId)
                    .Select(g => new
                    {
                        CursoId = g.Key,
                        Average = Math.Round(
                            g.Count(x => x.Estado == EstadoAsistencia.Presente) * 100m / g.Count(),
                            2)
                    })
                    .ToDictionaryAsync(x => x.CursoId, x => (decimal?)x.Average);

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

                var studentsAtRiskByAverage = await _db.Calificaciones
                    .AsNoTracking()
                    .Where(x => courseIds.Contains(x.CursoId) && !x.Archivado)
                    .GroupBy(x => new { x.CursoId, x.AlumnoId })
                    .Where(g => g.Average(x => x.Nota) < 60)
                    .Select(g => new { g.Key.CursoId, g.Key.AlumnoId })
                    .ToListAsync();

                var studentsAtRiskByAttendance = await _db.Asistencias
                    .AsNoTracking()
                    .Where(x => courseIds.Contains(x.Clase.CursoId) && x.Clase.Estado != EstadoClase.Cancelada)
                    .GroupBy(x => new { x.Clase.CursoId, x.AlumnoId })
                    .Where(g => g.Count(x => x.Estado == EstadoAsistencia.Presente) * 100m / g.Count() < 70)
                    .Select(g => new { g.Key.CursoId, g.Key.AlumnoId })
                    .ToListAsync();

                var studentsAtRisk = studentsAtRiskByAverage
                    .Concat(studentsAtRiskByAttendance)
                    .GroupBy(x => x.CursoId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.AlumnoId).Distinct().Count());

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

                    course.AcademicAverage = academicAverages.GetValueOrDefault(course.Id);
                    course.AttendanceAverage = attendanceAverages.GetValueOrDefault(course.Id);
                    course.PendingCorrectionsCount = pendingCorrections.GetValueOrDefault(course.Id);
                    course.StudentsAtRiskCount = studentsAtRisk.GetValueOrDefault(course.Id);
                    course.HealthStatus = CourseHealthCalculator.Calculate(
                        course.AttendanceAverage,
                        course.AcademicAverage,
                        course.StudentsAtRiskCount,
                        course.CantidadProfesores > 0);
                    course.MainSignal = GetMainSignal(course);
                    course.RequiresAttention =
                        course.HealthStatus.Level != "normal" ||
                        course.StudentsCount < 5 ||
                        course.PendingCorrectionsCount > 0;
                }
            }

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                pageNumber,
                pageSize,
                total,
                items = data
            });
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
    }
}
