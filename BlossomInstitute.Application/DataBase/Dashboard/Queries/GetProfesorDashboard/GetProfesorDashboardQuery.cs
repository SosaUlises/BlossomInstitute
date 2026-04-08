using BlossomInstitute.Application.DataBase.Dashboard.Queries.ProfesoresModels;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Entidades.Tarea;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Dashboard.Queries.GetProfesorDashboard
{
    public class GetProfesorDashboardQuery : IGetProfesorDashboardQuery
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public GetProfesorDashboardQuery(
            IDataBaseService db,
            UserManager<UsuarioEntity> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(int userId, CancellationToken ct = default)
        {
            if (userId <= 0)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, "No autenticado");

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || !user.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Usuario inválido o inactivo");

            if (!await _userManager.IsInRoleAsync(user, "Profesor"))
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Acceso denegado");

            var profesor = await _db.Profesores
                .AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => new
                {
                    x.Id,
                    Nombre = x.Usuario.Nombre,
                    Apellido = x.Usuario.Apellido,
                    Dni = x.Usuario.Dni,
                    Email = x.Usuario.Email
                })
                .FirstOrDefaultAsync(ct);

            if (profesor == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Profesor no encontrado");

            TimeZoneInfo argentinaTimeZone;

            try
            {
                argentinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
            }
            catch
            {
                argentinaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");
            }

            var ahoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, argentinaTimeZone);
            var hoy = DateOnly.FromDateTime(ahoraLocal);

            var cursos = await _db.CursoProfesores
                .AsNoTracking()
                .Where(x => x.ProfesorId == profesor.Id)
                .Select(x => new ProfesorDashboardCursoItemModel
                {
                    CursoId = x.CursoId,
                    CursoNombre = x.Curso.Nombre,
                    Anio = x.Curso.Anio,
                    Descripcion = x.Curso.Descripcion,
                    Estado = x.Curso.Estado
                })
                .OrderBy(x => x.CursoNombre)
                .ToListAsync(ct);

            var cursoIds = cursos
                .Select(x => x.CursoId)
                .Distinct()
                .ToList();

            if (cursoIds.Count == 0)
            {
                return ResponseApiService.Response(StatusCodes.Status200OK, new ProfesorDashboardResponseModel
                {
                    ProfesorId = profesor.Id,
                    Nombre = profesor.Nombre,
                    Apellido = profesor.Apellido,
                    Dni = profesor.Dni,
                    Email = profesor.Email,
                    CantidadCursos = 0,
                    CantidadAlumnos = 0,
                    TareasPublicadasCount = 0,
                    EntregasPendientesCorreccionCount = 0,
                    Cursos = new List<ProfesorDashboardCursoItemModel>(),
                    ProximasClases = new List<ProfesorDashboardProximaClaseItemModel>(),
                    UltimasClases = new List<ProfesorDashboardUltimaClaseItemModel>(),
                    UltimasEntregas = new List<ProfesorDashboardUltimaEntregaItemModel>(),
                    ResumenPorCurso = new List<ProfesorDashboardResumenCursoItemModel>()
                });
            }

            var cantidadAlumnos = await _db.Matriculas
                .AsNoTracking()
                .Where(x => cursoIds.Contains(x.CursoId))
                .Select(x => x.AlumnoId)
                .Distinct()
                .CountAsync(ct);

            var horarios = await _db.CursoHorarios
                .AsNoTracking()
                .Where(x => cursoIds.Contains(x.CursoId))
                .Select(x => new
                {
                    x.CursoId,
                    CursoNombre = x.Curso.Nombre,
                    x.Dia,
                    x.HoraInicio,
                    x.HoraFin
                })
                .ToListAsync(ct);

            var proximasClases = horarios
                .Select(h =>
                {
                    var proximaFecha = ObtenerProximaFecha(h.Dia, hoy, h.HoraInicio, ahoraLocal);

                    return new ProfesorDashboardProximaClaseItemModel
                    {
                        CursoId = h.CursoId,
                        CursoNombre = h.CursoNombre,
                        Dia = h.Dia,
                        Fecha = proximaFecha,
                        HoraInicio = h.HoraInicio,
                        HoraFin = h.HoraFin
                    };
                })
                .OrderBy(x => x.Fecha)
                .ThenBy(x => x.HoraInicio)
                .ThenBy(x => x.CursoNombre)
                .Take(5)
                .ToList();

            var ultimasClases = await _db.Clases
                .AsNoTracking()
                .Where(x =>
                    cursoIds.Contains(x.CursoId) &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha <= hoy)
                .OrderByDescending(x => x.Fecha)
                .ThenByDescending(x => x.Id)
                .Take(5)
                .Select(x => new ProfesorDashboardUltimaClaseItemModel
                {
                    ClaseId = x.Id,
                    CursoId = x.CursoId,
                    CursoNombre = x.Curso.Nombre,
                    Fecha = x.Fecha,
                    EstadoClase = x.Estado,
                    Descripcion = x.Descripcion
                })
                .ToListAsync(ct);

            var tareasPublicadasBaseQuery = _db.Tareas
                .AsNoTracking()
                .Where(x =>
                    cursoIds.Contains(x.CursoId) &&
                    x.Estado == EstadoTarea.Publicada);

            var tareasPublicadasCount = await tareasPublicadasBaseQuery.CountAsync(ct);

            var ultimasEntregas = await _db.Entregas
                .AsNoTracking()
                .Where(x => cursoIds.Contains(x.Tarea.CursoId))
                .OrderByDescending(x => x.FechaEntregaUtc)
                .Take(5)
                .Select(x => new ProfesorDashboardUltimaEntregaItemModel
                {
                    EntregaId = x.Id,
                    TareaId = x.TareaId,
                    CursoId = x.Tarea.CursoId,
                    CursoNombre = x.Tarea.Curso.Nombre,
                    TituloTarea = x.Tarea.Titulo,
                    AlumnoId = x.AlumnoId,
                    AlumnoNombre = x.Alumno.Usuario.Nombre,
                    AlumnoApellido = x.Alumno.Usuario.Apellido,
                    FechaEntregaUtc = x.FechaEntregaUtc,
                    EstadoEntrega = x.Estado
                })
                .ToListAsync(ct);

            var entregasPendientesCorreccionCount = await _db.Entregas
                .AsNoTracking()
                .Where(x =>
                    cursoIds.Contains(x.Tarea.CursoId) &&
                    !_db.EntregaFeedbacks.Any(f => f.EntregaId == x.Id && f.EsVigente))
                .CountAsync(ct);

            var alumnosPorCurso = await _db.Matriculas
                .AsNoTracking()
                .Where(x => cursoIds.Contains(x.CursoId))
                .GroupBy(x => x.CursoId)
                .Select(g => new
                {
                    CursoId = g.Key,
                    Cantidad = g.Select(x => x.AlumnoId).Distinct().Count()
                })
                .ToListAsync(ct);

            var tareasPublicadasPorCurso = await tareasPublicadasBaseQuery
                .GroupBy(x => x.CursoId)
                .Select(g => new
                {
                    CursoId = g.Key,
                    Cantidad = g.Count()
                })
                .ToListAsync(ct);

            var entregasPendientesCorreccionPorCurso = await _db.Entregas
                .AsNoTracking()
                .Where(x =>
                    cursoIds.Contains(x.Tarea.CursoId) &&
                    !_db.EntregaFeedbacks.Any(f => f.EntregaId == x.Id && f.EsVigente))
                .GroupBy(x => x.Tarea.CursoId)
                .Select(g => new
                {
                    CursoId = g.Key,
                    Cantidad = g.Count()
                })
                .ToListAsync(ct);

            var promedioCurso = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    cursoIds.Contains(x.CursoId) &&
                    !x.Archivado)
                .GroupBy(x => x.CursoId)
                .Select(g => new
                {
                    CursoId = g.Key,
                    Promedio = (decimal?)g.Average(x => x.Nota)
                })
                .ToListAsync(ct);

            var alumnosPorCursoDict = alumnosPorCurso.ToDictionary(x => x.CursoId, x => x.Cantidad);
            var tareasPublicadasPorCursoDict = tareasPublicadasPorCurso.ToDictionary(x => x.CursoId, x => x.Cantidad);
            var entregasPendientesCorreccionPorCursoDict = entregasPendientesCorreccionPorCurso.ToDictionary(x => x.CursoId, x => x.Cantidad);
            var promedioCursoDict = promedioCurso.ToDictionary(x => x.CursoId, x => x.Promedio);

            var resumenPorCurso = cursos
                .Select(c =>
                {
                    alumnosPorCursoDict.TryGetValue(c.CursoId, out var alumnosCurso);
                    tareasPublicadasPorCursoDict.TryGetValue(c.CursoId, out var tareasCurso);
                    entregasPendientesCorreccionPorCursoDict.TryGetValue(c.CursoId, out var pendientesCurso);
                    promedioCursoDict.TryGetValue(c.CursoId, out var promedio);

                    return new ProfesorDashboardResumenCursoItemModel
                    {
                        CursoId = c.CursoId,
                        CursoNombre = c.CursoNombre,
                        CantidadAlumnos = alumnosCurso,
                        TareasPublicadas = tareasCurso,
                        EntregasPendientesCorreccion = pendientesCurso,
                        PromedioCurso = promedio.HasValue ? Math.Round(promedio.Value, 2) : null
                    };
                })
                .OrderBy(x => x.CursoNombre)
                .ToList();

            var response = new ProfesorDashboardResponseModel
            {
                ProfesorId = profesor.Id,
                Nombre = profesor.Nombre,
                Apellido = profesor.Apellido,
                Dni = profesor.Dni,
                Email = profesor.Email,
                CantidadCursos = cursos.Count,
                CantidadAlumnos = cantidadAlumnos,
                TareasPublicadasCount = tareasPublicadasCount,
                EntregasPendientesCorreccionCount = entregasPendientesCorreccionCount,
                Cursos = cursos,
                ProximasClases = proximasClases,
                UltimasClases = ultimasClases,
                UltimasEntregas = ultimasEntregas,
                ResumenPorCurso = resumenPorCurso
            };

            return ResponseApiService.Response(StatusCodes.Status200OK, response);
        }

        private static DateOnly ObtenerProximaFecha(
            DayOfWeek diaClase,
            DateOnly hoy,
            TimeOnly horaInicio,
            DateTime ahoraLocal)
        {
            var diasHasta = ((int)diaClase - (int)hoy.DayOfWeek + 7) % 7;
            var fecha = hoy.AddDays(diasHasta);

            if (diasHasta == 0 && horaInicio <= TimeOnly.FromDateTime(ahoraLocal))
                fecha = fecha.AddDays(7);

            return fecha;
        }
    }
}