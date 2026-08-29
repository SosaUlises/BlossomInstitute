using BlossomInstitute.Application.DataBase.Dashboard.Queries.AlumnosModels;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Entidades.Entrega;
using BlossomInstitute.Domain.Entidades.Tarea;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Dashboard.Queries.GetAlumnoDashboard
{
    public class GetAlumnoDashboardQuery : IGetAlumnoDashboardQuery
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public GetAlumnoDashboardQuery(
            IDataBaseService db,
            UserManager<UsuarioEntity> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(int userId, CancellationToken ct = default)
        {
            if (userId <= 0)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "No autenticado");

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || !user.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Usuario inválido o inactivo");

            if (!await _userManager.IsInRoleAsync(user, "Alumno"))
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Acceso denegado");

            var alumno = await _db.Alumnos
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

            if (alumno == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Alumno no encontrado");

            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var ahoraLocal = DateTime.Now;
            var ahoraUtc = DateTime.UtcNow;

            var cursos = await _db.Matriculas
                .AsNoTracking()
                .Where(x => x.AlumnoId == alumno.Id)
                .Select(x => new DashboardCursoItemModel
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
                return ResponseApiService.Response(StatusCodes.Status200OK, new AlumnoDashboardResponseModel
                {
                    AlumnoId = alumno.Id,
                    Nombre = alumno.Nombre,
                    Apellido = alumno.Apellido,
                    Dni = alumno.Dni,
                    Email = alumno.Email,
                    CantidadCursos = 0,
                    TareasPendientesCount = 0,
                    EntregasRealizadasCount = 0,
                    FeedbacksRehacerCount = 0,
                    FeedbacksPendientesAccionCount = 0,
                    PromedioGeneral = null,
                    PorcentajeAsistenciaGeneral = 0,
                    Cursos = new List<DashboardCursoItemModel>(),
                    ProximasClases = new List<DashboardProximaClaseItemModel>(),
                    UltimasClases = new List<DashboardUltimaClaseItemModel>(),
                    TareasPendientes = new List<DashboardTareaPendienteItemModel>(),
                    UltimasEntregas = new List<DashboardUltimaEntregaItemModel>(),
                    UltimasCalificaciones = new List<DashboardUltimaCalificacionItemModel>(),
                    ResumenPorCurso = new List<DashboardResumenCursoItemModel>()
                });
            }

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

                    return new DashboardProximaClaseItemModel
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
                .Select(x => new DashboardUltimaClaseItemModel
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

            var tareasPendientesBaseQuery = tareasPublicadasBaseQuery
                .Where(x =>
                    x.FechaEntregaUtc.HasValue &&
                    !_db.Entregas.Any(e => e.TareaId == x.Id && e.AlumnoId == alumno.Id));

            var tareasPendientesCount = await tareasPendientesBaseQuery.CountAsync(ct);

            var tareasPendientes = await tareasPendientesBaseQuery
                .OrderBy(x => x.FechaEntregaUtc)
                .ThenBy(x => x.Titulo)
                .Take(5)
                .Select(x => new DashboardTareaPendienteItemModel
                {
                    TareaId = x.Id,
                    CursoId = x.CursoId,
                    CursoNombre = x.Curso.Nombre,
                    Titulo = x.Titulo,
                    FechaEntregaUtc = x.FechaEntregaUtc,
                    Vencida = x.FechaEntregaUtc.HasValue && x.FechaEntregaUtc.Value < ahoraUtc
                })
                .ToListAsync(ct);

            var ultimasEntregas = await _db.Entregas
                .AsNoTracking()
                .Where(x =>
                    x.AlumnoId == alumno.Id &&
                    cursoIds.Contains(x.Tarea.CursoId))
                .OrderByDescending(x => x.FechaEntregaUtc)
                .Take(5)
                .Select(x => new DashboardUltimaEntregaItemModel
                {
                    EntregaId = x.Id,
                    TareaId = x.TareaId,
                    CursoId = x.Tarea.CursoId,
                    CursoNombre = x.Tarea.Curso.Nombre,
                    TituloTarea = x.Tarea.Titulo,
                    FechaEntregaUtc = x.FechaEntregaUtc,
                    EstadoEntrega = x.Estado
                })
                .ToListAsync(ct);

            var entregasRealizadasCount = await _db.Entregas
                .AsNoTracking()
                .CountAsync(x =>
                    x.AlumnoId == alumno.Id &&
                    cursoIds.Contains(x.Tarea.CursoId), ct);

            var feedbacksVigentesPendientes = await _db.EntregaFeedbacks
                .AsNoTracking()
                .Where(x =>
                    x.EsVigente &&
                    x.Entrega.AlumnoId == alumno.Id &&
                    cursoIds.Contains(x.Entrega.Tarea.CursoId) &&
                    (x.Estado == EstadoCorreccion.Rehacer))
                .GroupBy(x => x.Estado)
                .Select(g => new
                {
                    Estado = g.Key,
                    Cantidad = g.Count()
                })
                .ToListAsync(ct);


            var feedbacksRehacerCount = feedbacksVigentesPendientes
                .Where(x => x.Estado == EstadoCorreccion.Rehacer)
                .Select(x => x.Cantidad)
                .FirstOrDefault();

            var feedbacksPendientesAccionCount = feedbacksRehacerCount;

            var feedbacksRecientes = await _db.EntregaFeedbacks
                .AsNoTracking()
                .Where(x =>
                    x.EsVigente &&
                    x.Entrega.AlumnoId == alumno.Id &&
                    cursoIds.Contains(x.Entrega.Tarea.CursoId))
                .OrderByDescending(x => x.FechaCorreccionUtc)
                .Take(5)
                .Select(x => new DashboardFeedbackRecienteItemModel
                {
                    CursoId = x.Entrega.Tarea.CursoId,
                    CursoNombre = x.Entrega.Tarea.Curso.Nombre,
                    TareaId = x.Entrega.TareaId,
                    TituloTarea = x.Entrega.Tarea.Titulo,
                    ProfesorNombre = x.Entrega.Tarea.Profesor.Usuario.Nombre,
                    ProfesorApellido = x.Entrega.Tarea.Profesor.Usuario.Apellido,
                    ProfesorAvatarUrl = x.Entrega.Tarea.Profesor.Usuario.AvatarUrl,
                    EntregaId = x.EntregaId,
                    FeedbackId = x.Id,
                    Estado = (int)x.Estado,
                    Comentario = x.Comentario,
                    Nota = x.Nota,
                    FechaCorreccionUtc = x.FechaCorreccionUtc
                })
                .ToListAsync(ct);

            var calificacionesBaseQuery = _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    x.AlumnoId == alumno.Id &&
                    cursoIds.Contains(x.CursoId) &&
                    !x.Archivado);

            var ultimasCalificaciones = await calificacionesBaseQuery
                .OrderByDescending(x => x.Fecha)
                .ThenByDescending(x => x.Id)
                .Take(5)
                .Select(x => new DashboardUltimaCalificacionItemModel
                {
                    CalificacionId = x.Id,
                    CursoId = x.CursoId,
                    CursoNombre = x.Curso.Nombre,
                    Tipo = x.Tipo,
                    Titulo = x.Titulo,
                    Nota = x.Nota,
                    Fecha = x.Fecha
                })
                .ToListAsync(ct);

            var promedioGeneral = await calificacionesBaseQuery
                .Select(x => (decimal?)x.Nota)
                .AverageAsync(ct);

            var clasesTomadasPorCurso = await _db.Clases
                .AsNoTracking()
                .Where(x =>
                    cursoIds.Contains(x.CursoId) &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha <= hoy)
                .GroupBy(x => x.CursoId)
                .Select(g => new
                {
                    CursoId = g.Key,
                    TotalClases = g.Count()
                })
                .ToListAsync(ct);

            var presentesPorCurso = await _db.Asistencias
                .AsNoTracking()
                .Where(x =>
                    x.AlumnoId == alumno.Id &&
                    cursoIds.Contains(x.Clase.CursoId) &&
                    x.Clase.Estado != EstadoClase.Cancelada &&
                    x.Clase.Fecha <= hoy &&
                    x.Estado == EstadoAsistencia.Presente)
                .GroupBy(x => x.Clase.CursoId)
                .Select(g => new
                {
                    CursoId = g.Key,
                    Presentes = g.Count()
                })
                .ToListAsync(ct);

            var promedioPorCurso = await calificacionesBaseQuery
                .GroupBy(x => x.CursoId)
                .Select(g => new
                {
                    CursoId = g.Key,
                    Promedio = (decimal?)g.Average(x => x.Nota)
                })
                .ToListAsync(ct);

            var tareasPendientesPorCurso = await tareasPendientesBaseQuery
                .GroupBy(x => x.CursoId)
                .Select(g => new
                {
                    CursoId = g.Key,
                    Cantidad = g.Count()
                })
                .ToListAsync(ct);

            var clasesTomadasPorCursoDict = clasesTomadasPorCurso.ToDictionary(x => x.CursoId, x => x.TotalClases);
            var presentesPorCursoDict = presentesPorCurso.ToDictionary(x => x.CursoId, x => x.Presentes);
            var promedioPorCursoDict = promedioPorCurso.ToDictionary(x => x.CursoId, x => x.Promedio);
            var tareasPendientesPorCursoDict = tareasPendientesPorCurso.ToDictionary(x => x.CursoId, x => x.Cantidad);

            var resumenPorCurso = cursos
                .Select(c =>
                {
                    clasesTomadasPorCursoDict.TryGetValue(c.CursoId, out var totalClases);
                    presentesPorCursoDict.TryGetValue(c.CursoId, out var presentes);
                    promedioPorCursoDict.TryGetValue(c.CursoId, out var promedioCurso);
                    tareasPendientesPorCursoDict.TryGetValue(c.CursoId, out var pendientesCurso);

                    var porcentajeAsistencia = totalClases > 0
                        ? Math.Round((decimal)presentes * 100 / totalClases, 2)
                        : 0;

                    return new DashboardResumenCursoItemModel
                    {
                        CursoId = c.CursoId,
                        CursoNombre = c.CursoNombre,
                        Promedio = promedioCurso.HasValue ? Math.Round(promedioCurso.Value, 2) : null,
                        PorcentajeAsistencia = porcentajeAsistencia,
                        TareasPendientes = pendientesCurso
                    };
                })
                .OrderBy(x => x.CursoNombre)
                .ToList();

            var totalClasesGeneral = clasesTomadasPorCurso.Sum(x => x.TotalClases);
            var presentesGeneral = presentesPorCurso.Sum(x => x.Presentes);

            var porcentajeAsistenciaGeneral = totalClasesGeneral > 0
                ? Math.Round((decimal)presentesGeneral * 100 / totalClasesGeneral, 2)
                : 0;

            var response = new AlumnoDashboardResponseModel
            {
                AlumnoId = alumno.Id,
                Nombre = alumno.Nombre,
                Apellido = alumno.Apellido,
                Dni = alumno.Dni,
                Email = alumno.Email,
                CantidadCursos = cursos.Count,
                TareasPendientesCount = tareasPendientesCount,
                EntregasRealizadasCount = entregasRealizadasCount,
                FeedbacksRehacerCount = feedbacksRehacerCount,
                FeedbacksPendientesAccionCount = feedbacksPendientesAccionCount,
                PromedioGeneral = promedioGeneral.HasValue ? Math.Round(promedioGeneral.Value, 2) : null,
                PorcentajeAsistenciaGeneral = porcentajeAsistenciaGeneral,
                Cursos = cursos,
                ProximasClases = proximasClases,
                UltimasClases = ultimasClases,
                TareasPendientes = tareasPendientes,
                UltimasEntregas = ultimasEntregas,
                UltimasCalificaciones = ultimasCalificaciones,
                FeedbacksRecientes = feedbacksRecientes,
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
