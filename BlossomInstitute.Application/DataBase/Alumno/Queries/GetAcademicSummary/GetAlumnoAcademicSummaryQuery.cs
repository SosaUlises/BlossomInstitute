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
        private readonly IDataBaseService _db;

        public GetAlumnoAcademicSummaryQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int alumnoId, CancellationToken ct)
        {
            if (alumnoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Alumno invalido");

            var alumno = await _db.Alumnos
                .AsNoTracking()
                .Where(x => x.Id == alumnoId)
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

            if (alumno == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Alumno no encontrado");

            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var contextoPeriodo = PeriodoAcademicoHelper.ObtenerContexto(hoy);
            var trimestre = contextoPeriodo.TrimestreActual;
            var fechaHasta = contextoPeriodo.Hasta;
            var desdeUtc = trimestre.Desde.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var hastaUtcExclusivo = fechaHasta.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var periodo = new AlumnoAcademicPeriodModel
            {
                Type = "academic-quarter",
                Label = trimestre.Etiqueta,
                MonthRangeLabel = trimestre.EtiquetaRangoMeses,
                From = contextoPeriodo.Desde,
                To = contextoPeriodo.Hasta,
                Year = contextoPeriodo.Anio,
                Quarter = contextoPeriodo.NumeroTrimestre
            };

            var matriculas = await _db.Matriculas
                .AsNoTracking()
                .Where(x => x.AlumnoId == alumnoId)
                .Select(x => new MatriculaAlumnoProjection
                {
                    CursoId = x.CursoId,
                    NombreCurso = x.Curso.Nombre,
                    DescripcionCurso = x.Curso.Descripcion,
                    EstadoCurso = x.Curso.Estado
                })
                .OrderBy(x => x.EstadoCurso == EstadoCurso.Activo ? 0 : 1)
                .ThenBy(x => x.NombreCurso)
                .ToListAsync(ct);

            var cursoIds = matriculas.Select(x => x.CursoId).Distinct().ToList();
            var profesores = cursoIds.Count == 0
                ? new List<ProfesorCursoProjection>()
                : await _db.CursoProfesores
                    .AsNoTracking()
                    .Where(x => cursoIds.Contains(x.CursoId))
                    .Select(x => new ProfesorCursoProjection
                    {
                        CursoId = x.CursoId,
                        Nombre = x.Profesor.Usuario.Nombre,
                        Apellido = x.Profesor.Usuario.Apellido,
                        AvatarUrl = x.Profesor.Usuario.AvatarUrl
                    })
                    .OrderBy(x => x.Apellido)
                    .ThenBy(x => x.Nombre)
                    .ToListAsync(ct);

            var profesoresPorCurso = profesores
                .GroupBy(x => x.CursoId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(profesor => new ProfesorResumenProjection
                    {
                        NombreCompleto = $"{profesor.Nombre} {profesor.Apellido}".Trim(),
                        AvatarUrl = profesor.AvatarUrl
                    }).First());

            var matriculasActuales = matriculas
                .Where(x => x.EstadoCurso == EstadoCurso.Activo)
                .Select((x, index) => ConstruirMatriculaModel(x, profesoresPorCurso, index == 0))
                .ToList();

            var cursoIdsActuales = matriculasActuales.Select(x => x.CourseId).ToList();
            var resumenAsistencia = new AlumnoAcademicAttendanceSummaryModel();
            var resumenCalificaciones = new AlumnoAcademicGradesSummaryModel();
            var resumenTareas = new AlumnoAcademicHomeworkSummaryModel();
            DateOnly? fechaUltimaAusencia = null;

            if (cursoIdsActuales.Count > 0)
            {
                var clases = await _db.Clases
                    .AsNoTracking()
                    .Where(x =>
                        cursoIdsActuales.Contains(x.CursoId) &&
                        x.Estado != EstadoClase.Cancelada &&
                        x.Fecha >= trimestre.Desde &&
                        x.Fecha <= fechaHasta)
                    .Select(x => new ClaseAlumnoProjection
                    {
                        Id = x.Id,
                        CursoId = x.CursoId,
                        Fecha = x.Fecha
                    })
                    .OrderBy(x => x.Fecha)
                    .ThenBy(x => x.Id)
                    .ToListAsync(ct);

                var claseIds = clases.Select(x => x.Id).ToList();
                var asistencias = claseIds.Count == 0
                    ? new List<AsistenciaAlumnoProjection>()
                    : await _db.Asistencias
                        .AsNoTracking()
                        .Where(x => x.AlumnoId == alumnoId && claseIds.Contains(x.ClaseId))
                        .Select(x => new AsistenciaAlumnoProjection
                        {
                            ClaseId = x.ClaseId,
                            Estado = x.Estado,
                            Fecha = x.Clase.Fecha
                        })
                        .ToListAsync(ct);

                resumenAsistencia = ConstruirResumenAsistencia(clases, asistencias, out fechaUltimaAusencia);

                var calificaciones = await _db.Calificaciones
                    .AsNoTracking()
                    .Where(x =>
                        x.AlumnoId == alumnoId &&
                        cursoIdsActuales.Contains(x.CursoId) &&
                        !x.Archivado &&
                        x.Fecha >= trimestre.Desde &&
                        x.Fecha <= fechaHasta &&
                        (
                            x.Tipo == TipoCalificacion.Homework ||
                            x.Tipo == TipoCalificacion.Quiz ||
                            x.Tipo == TipoCalificacion.Test ||
                            x.Tipo == TipoCalificacion.Participation ||
                            x.Tipo == TipoCalificacion.Behaviour
                        ))
                    .Select(x => new CalificacionAlumnoProjection
                    {
                        Id = x.Id,
                        CursoId = x.CursoId,
                        NombreCurso = x.Curso.Nombre,
                        Titulo = x.Titulo,
                        Tipo = x.Tipo,
                        Nota = x.Nota,
                        Fecha = x.Fecha
                    })
                    .ToListAsync(ct);

                resumenCalificaciones = ConstruirResumenCalificaciones(calificaciones);
                resumenTareas = await ConstruirResumenTareasAsync(alumnoId, cursoIdsActuales, desdeUtc, hastaUtcExclusivo, ct);
            }

            var estadoAcademico = ConstruirEstadoAcademico(resumenAsistencia, resumenCalificaciones);
            var senalesRecientes = ConstruirSenalesRecientes(resumenAsistencia, resumenCalificaciones, resumenTareas, fechaUltimaAusencia);
            var seguimientosPendientes = cursoIds.Count == 0
                ? new List<AlumnoAcademicPendingFollowUpModel>()
                : await ConstruirSeguimientosPendientesAsync(
                    alumnoId,
                    cursoIds,
                    matriculas.ToDictionary(x => x.CursoId, x => x.NombreCurso),
                    trimestre.Desde,
                    contextoPeriodo.TrimestreAnterior,
                    ct);

            var nombreCompleto = $"{alumno.Nombre} {alumno.Apellido}".Trim();
            var response = new AlumnoAcademicSummaryResponseModel
            {
                Student = new AlumnoAcademicIdentityModel
                {
                    Id = alumno.Id,
                    FirstName = alumno.Nombre,
                    LastName = alumno.Apellido,
                    FullName = nombreCompleto,
                    Email = alumno.Email,
                    Dni = alumno.Dni,
                    Active = alumno.Activo,
                    AvatarUrl = alumno.AvatarUrl
                },
                Period = periodo,
                CurrentCourse = matriculasActuales.FirstOrDefault(),
                CurrentEnrollments = matriculasActuales,
                AttendanceSummary = resumenAsistencia,
                GradesSummary = resumenCalificaciones,
                HomeworkSummary = resumenTareas,
                AcademicStatus = estadoAcademico,
                PendingFollowUp = seguimientosPendientes,
                RecentSignals = senalesRecientes
            };

            return ResponseApiService.Response(StatusCodes.Status200OK, response);
        }

        private static AlumnoAcademicEnrollmentModel ConstruirMatriculaModel(
            MatriculaAlumnoProjection matricula,
            Dictionary<int, ProfesorResumenProjection> profesoresPorCurso,
            bool esPrincipal)
        {
            profesoresPorCurso.TryGetValue(matricula.CursoId, out var profesor);

            return new AlumnoAcademicEnrollmentModel
            {
                CourseId = matricula.CursoId,
                CourseName = matricula.NombreCurso,
                CourseDescription = matricula.DescripcionCurso,
                CourseStatus = matricula.EstadoCurso.ToString(),
                TeacherName = profesor?.NombreCompleto,
                TeacherAvatarUrl = profesor?.AvatarUrl,
                IsMain = esPrincipal
            };
        }

        private async Task<List<AlumnoAcademicPendingFollowUpModel>> ConstruirSeguimientosPendientesAsync(
            int alumnoId,
            List<int> cursoIds,
            Dictionary<int, string> nombresCurso,
            DateOnly desdePeriodoActual,
            PeriodoAcademicoTrimestre trimestreAnterior,
            CancellationToken ct)
        {
            var clasesHistoricas = await _db.Clases
                .AsNoTracking()
                .Where(x =>
                    cursoIds.Contains(x.CursoId) &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha < desdePeriodoActual)
                .Select(x => new ClaseAlumnoProjection
                {
                    Id = x.Id,
                    CursoId = x.CursoId,
                    Fecha = x.Fecha
                })
                .ToListAsync(ct);

            var claseHistoricaIds = clasesHistoricas.Select(x => x.Id).ToList();
            var asistenciasHistoricas = claseHistoricaIds.Count == 0
                ? new List<AsistenciaAlumnoProjection>()
                : await _db.Asistencias
                    .AsNoTracking()
                    .Where(x => x.AlumnoId == alumnoId && claseHistoricaIds.Contains(x.ClaseId))
                    .Select(x => new AsistenciaAlumnoProjection
                    {
                        ClaseId = x.ClaseId,
                        Fecha = x.Clase.Fecha,
                        Estado = x.Estado
                    })
                    .ToListAsync(ct);

            var calificacionesHistoricas = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    x.AlumnoId == alumnoId &&
                    cursoIds.Contains(x.CursoId) &&
                    !x.Archivado &&
                    x.Fecha < desdePeriodoActual &&
                    (
                        x.Tipo == TipoCalificacion.Homework ||
                        x.Tipo == TipoCalificacion.Quiz ||
                        x.Tipo == TipoCalificacion.Test ||
                        x.Tipo == TipoCalificacion.Participation ||
                        x.Tipo == TipoCalificacion.Behaviour
                    ))
                .Select(x => new CalificacionAlumnoProjection
                {
                    Id = x.Id,
                    CursoId = x.CursoId,
                    NombreCurso = x.Curso.Nombre,
                    Titulo = x.Titulo,
                    Tipo = x.Tipo,
                    Nota = x.Nota,
                    Fecha = x.Fecha
                })
                .ToListAsync(ct);

            return ConstruirSeguimientosPendientes(
                nombresCurso,
                clasesHistoricas,
                asistenciasHistoricas,
                calificacionesHistoricas,
                trimestreAnterior);
        }

        private static List<AlumnoAcademicPendingFollowUpModel> ConstruirSeguimientosPendientes(
            Dictionary<int, string> nombresCurso,
            List<ClaseAlumnoProjection> clasesHistoricas,
            List<AsistenciaAlumnoProjection> asistenciasHistoricas,
            List<CalificacionAlumnoProjection> calificacionesHistoricas,
            PeriodoAcademicoTrimestre trimestreAnterior)
        {
            var clasePorId = clasesHistoricas.ToDictionary(x => x.Id);
            var cantidadClasesPorPeriodo = clasesHistoricas
                .GroupBy(x =>
                {
                    var periodo = PeriodoAcademicoHelper.ObtenerActual(x.Fecha);
                    return new SeguimientoPendienteKey(x.CursoId, periodo.Anio, periodo.Trimestre);
                })
                .ToDictionary(x => x.Key, x => x.Count());
            var acumuladores = new Dictionary<SeguimientoPendienteKey, SeguimientoPendienteAcumulador>();

            foreach (var grupo in calificacionesHistoricas.GroupBy(x =>
            {
                var periodo = PeriodoAcademicoHelper.ObtenerActual(x.Fecha);
                return new SeguimientoPendienteKey(x.CursoId, periodo.Anio, periodo.Trimestre);
            }))
            {
                var promedio = Math.Round(grupo.Average(x => x.Nota), 2);
                if (promedio >= ReglasAcademicas.UmbralPromedioSeguimiento)
                    continue;

                var periodo = PeriodoAcademicoHelper.ObtenerActual(grupo.First().Fecha);
                var nombreCurso = nombresCurso.GetValueOrDefault(grupo.Key.CursoId) ?? grupo.First().NombreCurso;
                var acumulador = ObtenerOCrearSeguimientoPendiente(acumuladores, grupo.Key, nombreCurso, periodo, trimestreAnterior);
                acumulador.Promedio = promedio;
                acumulador.Motivos.Add(promedio < ReglasAcademicas.UmbralNotaBaja ? "Promedio bajo" : "Promedio en seguimiento");
            }

            foreach (var grupo in asistenciasHistoricas
                .Where(x => clasePorId.ContainsKey(x.ClaseId))
                .GroupBy(x =>
                {
                    var clase = clasePorId[x.ClaseId];
                    var periodo = PeriodoAcademicoHelper.ObtenerActual(x.Fecha);
                    return new SeguimientoPendienteKey(clase.CursoId, periodo.Anio, periodo.Trimestre);
                }))
            {
                var key = grupo.Key;
                var cantidadClases = cantidadClasesPorPeriodo.GetValueOrDefault(key);
                if (cantidadClases <= 0)
                    continue;

                var asistencia = Math.Round(grupo.Count(x => x.Estado == EstadoAsistencia.Presente) * 100m / cantidadClases, 2);
                if (asistencia >= ReglasAcademicas.UmbralAsistenciaSeguimiento)
                    continue;

                var periodo = PeriodoAcademicoHelper.ObtenerActual(grupo.First().Fecha);
                var nombreCurso = nombresCurso.GetValueOrDefault(key.CursoId) ?? string.Empty;
                var acumulador = ObtenerOCrearSeguimientoPendiente(acumuladores, key, nombreCurso, periodo, trimestreAnterior);
                acumulador.Asistencia = asistencia;
                acumulador.Motivos.Add(asistencia < ReglasAcademicas.UmbralAsistenciaBaja ? "Baja asistencia" : "Asistencia en seguimiento");
            }

            return acumuladores.Values
                .Select(x => x.ToModel())
                .OrderByDescending(x => x.Level == "critical")
                .ThenByDescending(x => x.IsPreviousQuarter)
                .ThenByDescending(x => x.Year)
                .ThenByDescending(x => x.QuarterNumber)
                .ThenBy(x => x.CourseName)
                .ToList();
        }

        private static SeguimientoPendienteAcumulador ObtenerOCrearSeguimientoPendiente(
            Dictionary<SeguimientoPendienteKey, SeguimientoPendienteAcumulador> acumuladores,
            SeguimientoPendienteKey key,
            string nombreCurso,
            PeriodoAcademicoTrimestre periodo,
            PeriodoAcademicoTrimestre trimestreAnterior)
        {
            if (acumuladores.TryGetValue(key, out var acumulador))
                return acumulador;

            acumulador = new SeguimientoPendienteAcumulador
            {
                CursoId = key.CursoId,
                NombreCurso = nombreCurso,
                EtiquetaPeriodo = periodo.Etiqueta,
                NumeroTrimestre = periodo.Trimestre,
                Anio = periodo.Anio,
                EsTrimestreAnterior = periodo.Anio == trimestreAnterior.Anio &&
                    periodo.Trimestre == trimestreAnterior.Trimestre
            };
            acumuladores[key] = acumulador;

            return acumulador;
        }

        private static AlumnoAcademicAttendanceSummaryModel ConstruirResumenAsistencia(
            List<ClaseAlumnoProjection> clases,
            List<AsistenciaAlumnoProjection> asistencias,
            out DateOnly? fechaUltimaAusencia)
        {
            fechaUltimaAusencia = asistencias
                .Where(x => x.Estado == EstadoAsistencia.Ausente)
                .OrderByDescending(x => x.Fecha)
                .Select(x => (DateOnly?)x.Fecha)
                .FirstOrDefault();

            if (clases.Count == 0)
            {
                return new AlumnoAcademicAttendanceSummaryModel
                {
                    PresentCount = 0,
                    AbsentCount = 0,
                    TotalClasses = 0,
                    ConsecutiveAbsences = 0
                };
            }

            var presentes = asistencias.Count(x => x.Estado == EstadoAsistencia.Presente);
            var ausentes = asistencias.Count(x => x.Estado == EstadoAsistencia.Ausente);
            var porcentaje = Math.Round((decimal)presentes * 100 / clases.Count, 2);
            var maximoAusenciasConsecutivas = ObtenerMaximoAusenciasConsecutivas(clases, asistencias);

            return new AlumnoAcademicAttendanceSummaryModel
            {
                AttendancePercentage = porcentaje,
                PresentCount = presentes,
                AbsentCount = ausentes,
                TotalClasses = clases.Count,
                ConsecutiveAbsences = maximoAusenciasConsecutivas,
                IsLowAttendance = porcentaje < ReglasAcademicas.UmbralAsistenciaBaja
            };
        }

        private static int ObtenerMaximoAusenciasConsecutivas(
            List<ClaseAlumnoProjection> clases,
            List<AsistenciaAlumnoProjection> asistencias)
        {
            var asistenciaPorClase = asistencias
                .GroupBy(x => x.ClaseId)
                .ToDictionary(x => x.Key, x => x.First().Estado);

            var actual = 0;
            var maximo = 0;

            foreach (var clase in clases.OrderBy(x => x.Fecha).ThenBy(x => x.Id))
            {
                if (asistenciaPorClase.TryGetValue(clase.Id, out var estado) && estado == EstadoAsistencia.Ausente)
                {
                    actual++;
                    maximo = Math.Max(maximo, actual);
                    continue;
                }

                if (estado == EstadoAsistencia.Presente)
                    actual = 0;
            }

            return maximo;
        }

        private static AlumnoAcademicGradesSummaryModel ConstruirResumenCalificaciones(List<CalificacionAlumnoProjection> calificaciones)
        {
            if (calificaciones.Count == 0)
            {
                return new AlumnoAcademicGradesSummaryModel
                {
                    LowGradesCount = 0
                };
            }

            var calificacionesManuales = calificaciones
                .Where(x => x.Tipo != TipoCalificacion.Homework)
                .ToList();

            var ultimaNotaBaja = calificaciones
                .Where(x => x.Nota < ReglasAcademicas.UmbralNotaBaja)
                .OrderByDescending(x => x.Fecha)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();

            var ultimaNota = calificaciones
                .OrderByDescending(x => x.Fecha)
                .ThenByDescending(x => x.Id)
                .First();

            return new AlumnoAcademicGradesSummaryModel
            {
                AverageGrade = Math.Round(calificaciones.Average(x => x.Nota), 2),
                ManualAverageGrade = calificacionesManuales.Count > 0
                    ? Math.Round(calificacionesManuales.Average(x => x.Nota), 2)
                    : null,
                LowGradesCount = calificaciones.Count(x => x.Nota < ReglasAcademicas.UmbralNotaBaja),
                LatestLowGrade = ultimaNotaBaja == null ? null : ConvertirCalificacionSenalModel(ultimaNotaBaja),
                LatestGrade = ConvertirCalificacionSenalModel(ultimaNota)
            };
        }

        private async Task<AlumnoAcademicHomeworkSummaryModel> ConstruirResumenTareasAsync(
            int alumnoId,
            List<int> cursoIdsActuales,
            DateTime desdeUtc,
            DateTime hastaUtcExclusivo,
            CancellationToken ct)
        {
            var tareaIds = await _db.Tareas
                .AsNoTracking()
                .Where(x =>
                    cursoIdsActuales.Contains(x.CursoId) &&
                    x.Estado == EstadoTarea.Publicada &&
                    x.FechaEntregaUtc.HasValue &&
                    x.FechaEntregaUtc.Value >= desdeUtc &&
                    x.FechaEntregaUtc.Value < hastaUtcExclusivo)
                .Select(x => x.Id)
                .ToListAsync(ct);

            if (tareaIds.Count == 0)
            {
                return new AlumnoAcademicHomeworkSummaryModel
                {
                    PendingSubmissions = 0,
                    PendingCorrections = 0,
                    ApprovedCount = 0,
                    NeedsRevisionCount = 0
                };
            }

            var entregas = await _db.Entregas
                .AsNoTracking()
                .Where(x => x.AlumnoId == alumnoId && tareaIds.Contains(x.TareaId))
                .Select(x => new
                {
                    x.Id,
                    x.TareaId
                })
                .ToListAsync(ct);

            var entregaIds = entregas.Select(x => x.Id).ToList();
            var correcciones = entregaIds.Count == 0
                ? new List<CorreccionEntregaProjection>()
                : await _db.EntregaFeedbacks
                    .AsNoTracking()
                    .Where(x => entregaIds.Contains(x.EntregaId) && x.EsVigente)
                    .Select(x => new CorreccionEntregaProjection
                    {
                        EntregaId = x.EntregaId,
                        Estado = x.Estado
                    })
                    .ToListAsync(ct);

            return new AlumnoAcademicHomeworkSummaryModel
            {
                PendingSubmissions = tareaIds.Count - entregas.Select(x => x.TareaId).Distinct().Count(),
                PendingCorrections = entregas.Count - correcciones.Select(x => x.EntregaId).Distinct().Count(),
                ApprovedCount = correcciones.Count(x => x.Estado == EstadoCorreccion.Aprobado),
                NeedsRevisionCount = correcciones.Count(x => x.Estado == EstadoCorreccion.Rehacer)
            };
        }

        private static AlumnoAcademicStatusModel ConstruirEstadoAcademico(
            AlumnoAcademicAttendanceSummaryModel asistencia,
            AlumnoAcademicGradesSummaryModel calificaciones)
        {
            var asistenciaBaja = asistencia.AttendancePercentage.HasValue &&
                asistencia.AttendancePercentage.Value < ReglasAcademicas.UmbralAsistenciaBaja;
            var promedioBajo = calificaciones.AverageGrade.HasValue &&
                calificaciones.AverageGrade.Value < ReglasAcademicas.UmbralNotaBaja;
            var ausenciasConsecutivas = asistencia.ConsecutiveAbsences ?? 0;

            var motivos = new List<string>();

            if (asistenciaBaja)
                motivos.Add($"Asistencia trimestral {asistencia.AttendancePercentage:0.##}%");

            if (promedioBajo)
                motivos.Add($"Promedio trimestral {calificaciones.AverageGrade:0.##}");

            if (ausenciasConsecutivas >= ReglasAcademicas.AusenciasConsecutivasCriticas)
                motivos.Add($"{ausenciasConsecutivas} ausencias consecutivas");

            if ((asistenciaBaja && promedioBajo) || ausenciasConsecutivas >= ReglasAcademicas.AusenciasConsecutivasCriticas)
            {
                return new AlumnoAcademicStatusModel
                {
                    Level = "critical",
                    Label = "Requiere intervencion prioritaria",
                    Reasons = motivos
                };
            }

            if (asistenciaBaja || promedioBajo)
            {
                return new AlumnoAcademicStatusModel
                {
                    Level = "follow-up",
                    Label = "Requiere seguimiento",
                    Reasons = motivos
                };
            }

            return new AlumnoAcademicStatusModel
            {
                Level = "normal",
                Label = "Sin alertas academicas",
                Reasons = motivos
            };
        }

        private static List<AlumnoAcademicSignalModel> ConstruirSenalesRecientes(
            AlumnoAcademicAttendanceSummaryModel asistencia,
            AlumnoAcademicGradesSummaryModel calificaciones,
            AlumnoAcademicHomeworkSummaryModel tareas,
            DateOnly? fechaUltimaAusencia)
        {
            var senales = new List<AlumnoAcademicSignalModel>();

            if (calificaciones.LatestLowGrade != null)
            {
                senales.Add(new AlumnoAcademicSignalModel
                {
                    Type = "low-grade",
                    Title = "Nota baja",
                    Description = $"{calificaciones.LatestLowGrade.Title}: {calificaciones.LatestLowGrade.Grade:0.##}",
                    Severity = calificaciones.LatestLowGrade.Grade < ReglasAcademicas.UmbralNotaCritica ? "critical" : "attention",
                    Date = calificaciones.LatestLowGrade.Date
                });
            }

            if (asistencia.AttendancePercentage.HasValue &&
                asistencia.AttendancePercentage.Value < ReglasAcademicas.UmbralAsistenciaBaja)
            {
                senales.Add(new AlumnoAcademicSignalModel
                {
                    Type = "low-attendance",
                    Title = "Baja asistencia",
                    Description = $"Asistencia trimestral {asistencia.AttendancePercentage:0.##}%",
                    Severity = asistencia.AttendancePercentage.Value < ReglasAcademicas.UmbralAsistenciaCritica ? "critical" : "attention",
                    Date = fechaUltimaAusencia
                });
            }

            if ((asistencia.ConsecutiveAbsences ?? 0) >= ReglasAcademicas.AusenciasConsecutivasCriticas)
            {
                senales.Add(new AlumnoAcademicSignalModel
                {
                    Type = "consecutive-absences",
                    Title = "Ausencias consecutivas",
                    Description = $"{asistencia.ConsecutiveAbsences} ausencias consecutivas registradas",
                    Severity = "critical",
                    Date = fechaUltimaAusencia
                });
            }

            if ((tareas.PendingSubmissions ?? 0) > 0)
            {
                senales.Add(new AlumnoAcademicSignalModel
                {
                    Type = "missing-homework",
                    Title = "Entregas pendientes",
                    Description = $"{tareas.PendingSubmissions} tarea(s) sin entregar en el trimestre",
                    Severity = "attention"
                });
            }

            return senales
                .OrderByDescending(x => x.Severity == "critical")
                .ThenByDescending(x => x.Date)
                .Take(6)
                .ToList();
        }

        private static AlumnoAcademicGradeSignalModel ConvertirCalificacionSenalModel(CalificacionAlumnoProjection calificacion)
        {
            return new AlumnoAcademicGradeSignalModel
            {
                Id = calificacion.Id,
                CourseId = calificacion.CursoId,
                CourseName = calificacion.NombreCurso,
                Title = calificacion.Titulo,
                Type = calificacion.Tipo.ToString(),
                Grade = Math.Round(calificacion.Nota, 2),
                Date = calificacion.Fecha
            };
        }
    }
}
