using BlossomInstitute.Application.DataBase.Dashboard.Queries.ProfesoresModels;
using BlossomInstitute.Application.Common.Academic;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Calificacion;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Entidades.Curso;
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
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "No autenticado");

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || !user.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Usuario inválido o inactivo");

            if (!await _userManager.IsInRoleAsync(user, "Profesor"))
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Acceso denegado");

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
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Profesor no encontrado");

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
            var periodoAcademico = AcademicQuarterHelper.GetContext(hoy);

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
                    ResumenPorCurso = new List<ProfesorDashboardResumenCursoItemModel>(),
                    AlumnosQueRequierenAtencion = new List<ProfesorDashboardAlumnoAtencionItemModel>()
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

            var entregasDashboard = await _db.Entregas
              .AsNoTracking()
              .Where(x => cursoIds.Contains(x.Tarea.CursoId))
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
                  AlumnoAvatarUrl = x.Alumno.Usuario.AvatarUrl,
                  FechaEntregaUtc = x.FechaEntregaUtc,
                  EstadoEntrega = x.Estado,
                  TieneFeedbackVigente = x.Feedbacks.Any(f => f.EsVigente)
              })
              .ToListAsync(ct);

            var pendientesCorreccion = entregasDashboard
                .Where(x => !x.TieneFeedbackVigente)
                .OrderBy(x => x.FechaEntregaUtc)
                .Take(5)
                .ToList();

            var cupoRestante = Math.Max(0, 5 - pendientesCorreccion.Count);

            var recientesCorregidas = entregasDashboard
                .Where(x => x.TieneFeedbackVigente)
                .OrderByDescending(x => x.FechaEntregaUtc)
                .Take(cupoRestante)
                .ToList();

            var ultimasEntregas = pendientesCorreccion
                .Concat(recientesCorregidas)
                .ToList();

            var entregasPendientesCorreccionCount = await _db.Entregas
                .AsNoTracking()
                .Where(x =>
                    cursoIds.Contains(x.Tarea.CursoId) &&
                    !x.Feedbacks.Any(f => f.EsVigente))
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

            var calificacionesTrimestre = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    cursoIds.Contains(x.CursoId) &&
                    x.Curso.Estado == EstadoCurso.Activo &&
                    !x.Archivado &&
                    x.Fecha >= periodoAcademico.From &&
                    x.Fecha <= periodoAcademico.To)
                .Select(x => new
                {
                    x.AlumnoId,
                    AlumnoNombre = x.Alumno.Usuario.Nombre + " " + x.Alumno.Usuario.Apellido,
                    AlumnoAvatarUrl = x.Alumno.Usuario.AvatarUrl,
                    x.CursoId,
                    CursoNombre = x.Curso.Nombre,
                    x.Tipo,
                    x.Titulo,
                    x.Nota,
                    x.Fecha
                })
                .ToListAsync(ct);

            var clasesTrimestre = await _db.Clases
                .AsNoTracking()
                .Where(x =>
                    cursoIds.Contains(x.CursoId) &&
                    x.Curso.Estado == EstadoCurso.Activo &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha >= periodoAcademico.From &&
                    x.Fecha <= periodoAcademico.To)
                .Select(x => new { x.Id, x.CursoId })
                .ToListAsync(ct);

            var claseIdsTrimestre = clasesTrimestre.Select(x => x.Id).ToList();
            var asistenciasTrimestre = await _db.Asistencias
                .AsNoTracking()
                .Where(x => claseIdsTrimestre.Contains(x.ClaseId))
                .Select(x => new
                {
                    x.AlumnoId,
                    x.Clase.CursoId,
                    x.Estado
                })
                .ToListAsync(ct);

            var clasesPorCurso = clasesTrimestre
                .GroupBy(x => x.CursoId)
                .ToDictionary(x => x.Key, x => x.Count());

            var asistenciasPorAlumnoCurso = asistenciasTrimestre
                .GroupBy(x => new { x.AlumnoId, x.CursoId })
                .ToDictionary(
                    x => (x.Key.AlumnoId, x.Key.CursoId),
                    x => x.Count(a => a.Estado == EstadoAsistencia.Presente));

            var matriculasActivas = await _db.Matriculas
                .AsNoTracking()
                .Where(x =>
                    cursoIds.Contains(x.CursoId) &&
                    x.Curso.Estado == EstadoCurso.Activo)
                .Select(x => new
                {
                    x.AlumnoId,
                    AlumnoNombre = x.Alumno.Usuario.Nombre + " " + x.Alumno.Usuario.Apellido,
                    AlumnoAvatarUrl = x.Alumno.Usuario.AvatarUrl,
                    x.CursoId,
                    CursoNombre = x.Curso.Nombre
                })
                .ToListAsync(ct);

            var calificacionesPorAlumnoCurso = calificacionesTrimestre
                .GroupBy(x => new { x.AlumnoId, x.CursoId })
                .ToDictionary(x => (x.Key.AlumnoId, x.Key.CursoId), x => x.ToList());

            var alumnosQueRequierenAtencion = matriculasActivas
                .Select(matricula =>
                {
                    var calificaciones = calificacionesPorAlumnoCurso.GetValueOrDefault(
                        (matricula.AlumnoId, matricula.CursoId));
                    var promedio = calificaciones is { Count: > 0 }
                        ? Math.Round(calificaciones.Average(x => x.Nota), 2)
                        : (decimal?)null;
                    var totalClases = clasesPorCurso.GetValueOrDefault(matricula.CursoId);
                    var presentes = asistenciasPorAlumnoCurso.GetValueOrDefault(
                        (matricula.AlumnoId, matricula.CursoId));
                    var asistencia = totalClases > 0
                        ? Math.Round((decimal)presentes * 100 / totalClases, 2)
                        : (decimal?)null;
                    var calificacionBaja = calificaciones?
                        .Where(x =>
                            x.Nota < 60 &&
                            (
                                x.Tipo == TipoCalificacion.Quiz ||
                                x.Tipo == TipoCalificacion.Test ||
                                x.Tipo == TipoCalificacion.Participation ||
                                x.Tipo == TipoCalificacion.Behaviour
                            ))
                        .OrderBy(x => x.Nota)
                        .ThenByDescending(x => x.Fecha)
                        .FirstOrDefault();
                    var promedioBajo = promedio.HasValue && promedio.Value < 60;
                    var asistenciaBaja = asistencia.HasValue && asistencia.Value < 70;

                    if (!promedioBajo && !asistenciaBaja && calificacionBaja == null)
                        return null;

                    return new ProfesorDashboardAlumnoAtencionItemModel
                    {
                        AlumnoId = matricula.AlumnoId,
                        AlumnoNombre = matricula.AlumnoNombre,
                        AlumnoAvatarUrl = matricula.AlumnoAvatarUrl,
                        CursoId = matricula.CursoId,
                        CursoNombre = matricula.CursoNombre,
                        PeriodoLabel = periodoAcademico.Label,
                        Promedio = promedioBajo ? promedio : null,
                        Asistencia = asistenciaBaja ? asistencia : null,
                        CalificacionBajaTitulo = calificacionBaja?.Titulo,
                        CalificacionBajaTipo = calificacionBaja?.Tipo,
                        CalificacionBajaNota = calificacionBaja != null
                            ? Math.Round(calificacionBaja.Nota, 2)
                            : null,
                        Severidad = promedioBajo && asistenciaBaja ? "critical" : "attention"
                    };
                })
                .Where(x => x != null)
                .OrderByDescending(x => x!.Severidad == "critical")
                .ThenBy(x => x!.Asistencia ?? 100)
                .ThenBy(x => x!.Promedio ?? 100)
                .Take(3)
                .Select(x => x!)
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
                ResumenPorCurso = resumenPorCurso,
                AlumnosQueRequierenAtencion = alumnosQueRequierenAtencion
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
