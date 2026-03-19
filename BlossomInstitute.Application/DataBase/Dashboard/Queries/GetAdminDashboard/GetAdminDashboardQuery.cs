using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Calificacion;
using BlossomInstitute.Domain.Entidades.Curso;
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
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "UserId inválido");

            if (!isAdmin)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "No autorizado");

            var nowUtc = DateTime.UtcNow;
            var nowLocal = DateTime.Now;

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
            // GENERAL AVERAGE
            // -------------------------------------------------
            var generalAverageRaw = await _db.Calificaciones
                .AsNoTracking()
                .Where(x => !x.Archivado)
                .Select(x => (decimal?)x.Nota)
                .AverageAsync(ct);

            decimal? generalAverage = generalAverageRaw.HasValue
                ? Math.Round(generalAverageRaw.Value, 2)
                : null;

            // -------------------------------------------------
            // AVERAGE GRADES BY COURSE
            // -------------------------------------------------
            var averageGradesByCourse = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    !x.Archivado &&
                    x.Curso.Estado == EstadoCurso.Activo &&
                    (
                        x.Tipo == TipoCalificacion.Homework ||
                        x.Tipo == TipoCalificacion.Quiz ||
                        x.Tipo == TipoCalificacion.Test
                    ))
                .GroupBy(x => new { x.CursoId, x.Curso.Nombre })
                .Select(g => new DashboardAverageGradeByCourseModel
                {
                    CursoId = g.Key.CursoId,
                    CursoNombre = g.Key.Nombre,
                    AverageGrade = Math.Round(g.Average(x => x.Nota), 2)
                })
                .OrderByDescending(x => x.AverageGrade)
                .Take(8)
                .ToListAsync(ct);

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
                Overview = overview,
                GeneralAverage = generalAverage,
                AverageGradesByCourse = averageGradesByCourse,
                UpcomingAssignments = upcomingAssignments,
                UpcomingClasses = upcomingClasses
            };

            return ResponseApiService.Response(StatusCodes.Status200OK, response);
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
