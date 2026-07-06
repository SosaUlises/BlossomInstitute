using BlossomInstitute.Application.Common.Academico;
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

        public async Task<BaseResponseModel> Execute(
            int pageNumber,
            int pageSize,
            string? search,
            CancellationToken ct = default)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            search = search?.Trim();

            var rolAlumnoId = await _db.Roles
                .Where(r => r.Name == "Alumno")
                .Select(r => r.Id)
                .FirstOrDefaultAsync(ct);

            if (rolAlumnoId == 0)
                return ResponseApiService.Response(StatusCodes.Status500InternalServerError, message: "Rol Alumno no existe");

            var query = _db.Usuarios
                .AsNoTracking()
                .Where(usuario => _db.UserRoles
                    .AsNoTracking()
                    .Any(rolUsuario => rolUsuario.UserId == usuario.Id && rolUsuario.RoleId == rolAlumnoId));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var textoBusqueda = search.ToLowerInvariant();
                query = query.Where(usuario =>
                    (usuario.Nombre ?? "").ToLower().Contains(textoBusqueda) ||
                    (usuario.Apellido ?? "").ToLower().Contains(textoBusqueda) ||
                    (usuario.Email ?? "").ToLower().Contains(textoBusqueda) ||
                    usuario.Dni.ToString().Contains(textoBusqueda));
            }

            var total = await query.CountAsync(ct);

            var alumnos = await query
                .OrderBy(usuario => usuario.Apellido)
                .ThenBy(usuario => usuario.Nombre)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(usuario => new GetAlumnoModel
                {
                    Id = usuario.Id,
                    Email = usuario.Email!,
                    Nombre = usuario.Nombre!,
                    Apellido = usuario.Apellido!,
                    Dni = usuario.Dni,
                    Telefono = usuario.PhoneNumber ?? "",
                    Activo = usuario.Activo,
                    IsActive = usuario.Activo,
                    AvatarUrl = usuario.AvatarUrl
                })
                .ToListAsync(ct);

            await AgregarContextoAcademicoAsync(alumnos, ct);

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                pageNumber,
                pageSize,
                total,
                items = alumnos
            });
        }

        private async Task AgregarContextoAcademicoAsync(List<GetAlumnoModel> alumnos, CancellationToken ct)
        {
            if (alumnos.Count == 0)
                return;

            var alumnoIds = alumnos.Select(x => x.Id).ToList();
            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var contextoPeriodo = PeriodoAcademicoHelper.ObtenerContexto(hoy);
            var trimestre = contextoPeriodo.TrimestreActual;
            var fechaHasta = contextoPeriodo.Hasta;

            var matriculas = await _db.Matriculas
                .AsNoTracking()
                .Where(x =>
                    alumnoIds.Contains(x.AlumnoId) &&
                    x.Curso.Estado == EstadoCurso.Activo)
                .Select(x => new MatriculaAlumnoProjection
                {
                    AlumnoId = x.AlumnoId,
                    CursoId = x.CursoId,
                    NombreCurso = x.Curso.Nombre,
                    DescripcionCurso = x.Curso.Descripcion
                })
                .OrderBy(x => x.AlumnoId)
                .ThenBy(x => x.NombreCurso)
                .ToListAsync(ct);

            var matriculasPorAlumno = matriculas
                .GroupBy(x => x.AlumnoId)
                .ToDictionary(x => x.Key, x => x.ToList());

            foreach (var alumno in alumnos)
            {
                var matriculasAlumno = matriculasPorAlumno.GetValueOrDefault(alumno.Id) ?? new List<MatriculaAlumnoProjection>();
                var matriculaActual = matriculasAlumno.FirstOrDefault();

                alumno.CurrentCourseId = matriculaActual?.CursoId;
                alumno.CurrentCourseName = matriculaActual?.NombreCurso;
                alumno.CurrentCourseDescription = matriculaActual?.DescripcionCurso;
                alumno.HasActiveEnrollment = matriculaActual != null;
                alumno.IsWithoutCourse = matriculaActual == null;
            }

            var cursoIds = matriculas.Select(x => x.CursoId).Distinct().ToList();
            var clases = cursoIds.Count == 0
                ? new List<ClaseAlumnoProjection>()
                : await _db.Clases
                    .AsNoTracking()
                    .Where(x =>
                        cursoIds.Contains(x.CursoId) &&
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
                    .Where(x =>
                        alumnoIds.Contains(x.AlumnoId) &&
                        claseIds.Contains(x.ClaseId))
                    .Select(x => new AsistenciaAlumnoProjection
                    {
                        AlumnoId = x.AlumnoId,
                        ClaseId = x.ClaseId,
                        Estado = x.Estado
                    })
                    .ToListAsync(ct);

            var calificaciones = cursoIds.Count == 0
                ? new List<CalificacionAlumnoProjection>()
                : await _db.Calificaciones
                    .AsNoTracking()
                    .Where(x =>
                        alumnoIds.Contains(x.AlumnoId) &&
                        cursoIds.Contains(x.CursoId) &&
                        !x.Archivado &&
                        x.Fecha >= trimestre.Desde &&
                        x.Fecha <= fechaHasta)
                    .Select(x => new CalificacionAlumnoProjection
                    {
                        Id = x.Id,
                        AlumnoId = x.AlumnoId,
                        CursoId = x.CursoId,
                        NombreCurso = x.Curso.Nombre,
                        Titulo = x.Titulo,
                        Nota = x.Nota,
                        Fecha = x.Fecha
                    })
                    .ToListAsync(ct);

            var clasesPorCurso = clases
                .GroupBy(x => x.CursoId)
                .ToDictionary(x => x.Key, x => x.ToList());

            var asistenciasPorAlumno = asistencias
                .GroupBy(x => x.AlumnoId)
                .ToDictionary(x => x.Key, x => x.ToList());

            var calificacionesPorAlumno = calificaciones
                .GroupBy(x => x.AlumnoId)
                .ToDictionary(x => x.Key, x => x.ToList());

            foreach (var alumno in alumnos)
            {
                var matriculasAlumno = matriculasPorAlumno.GetValueOrDefault(alumno.Id) ?? new List<MatriculaAlumnoProjection>();
                var cursoIdsAlumno = matriculasAlumno.Select(x => x.CursoId).ToHashSet();
                var clasesAlumno = cursoIdsAlumno
                    .SelectMany(cursoId => clasesPorCurso.GetValueOrDefault(cursoId) ?? new List<ClaseAlumnoProjection>())
                    .OrderBy(x => x.Fecha)
                    .ThenBy(x => x.Id)
                    .ToList();
                var asistenciasAlumno = asistenciasPorAlumno.GetValueOrDefault(alumno.Id) ?? new List<AsistenciaAlumnoProjection>();

                AplicarAsistencia(alumno, clasesAlumno, asistenciasAlumno);
                AplicarCalificaciones(alumno, calificacionesPorAlumno.GetValueOrDefault(alumno.Id) ?? new List<CalificacionAlumnoProjection>());
                AplicarEstadoAcademico(alumno);
            }
        }

        private static void AplicarAsistencia(
            GetAlumnoModel alumno,
            List<ClaseAlumnoProjection> clases,
            List<AsistenciaAlumnoProjection> asistencias)
        {
            alumno.ConsecutiveAbsences = 0;

            if (clases.Count == 0)
                return;

            var presentes = asistencias.Count(x => x.Estado == EstadoAsistencia.Presente);
            alumno.AttendancePercentage = Math.Round((decimal)presentes * 100 / clases.Count, 2);
            alumno.ConsecutiveAbsences = ObtenerMaximoAusenciasConsecutivas(clases, asistencias);
        }

        private static void AplicarCalificaciones(GetAlumnoModel alumno, List<CalificacionAlumnoProjection> calificaciones)
        {
            if (calificaciones.Count == 0)
                return;

            alumno.AverageGrade = Math.Round(calificaciones.Average(x => x.Nota), 2);

            var ultimaNotaBaja = calificaciones
                .Where(x => x.Nota < ReglasAcademicas.UmbralNotaBaja)
                .OrderByDescending(x => x.Fecha)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();

            if (ultimaNotaBaja == null)
                return;

            alumno.LatestLowGrade = new GetAlumnoLatestLowGradeModel
            {
                Id = ultimaNotaBaja.Id,
                CourseId = ultimaNotaBaja.CursoId,
                CourseName = ultimaNotaBaja.NombreCurso,
                Title = ultimaNotaBaja.Titulo,
                Grade = Math.Round(ultimaNotaBaja.Nota, 2),
                Date = ultimaNotaBaja.Fecha
            };
        }

        private static void AplicarEstadoAcademico(GetAlumnoModel alumno)
        {
            var asistenciaBaja = alumno.AttendancePercentage.HasValue &&
                alumno.AttendancePercentage.Value < ReglasAcademicas.UmbralAsistenciaBaja;
            var promedioBajo = alumno.AverageGrade.HasValue &&
                alumno.AverageGrade.Value < ReglasAcademicas.UmbralNotaBaja;
            var tieneAusenciasConsecutivas = (alumno.ConsecutiveAbsences ?? 0) >= ReglasAcademicas.AusenciasConsecutivasCriticas;
            var tieneUltimaNotaBaja = alumno.LatestLowGrade != null;

            var motivos = new List<string>();

            if (alumno.IsWithoutCourse)
                motivos.Add("Sin curso activo");

            if (asistenciaBaja)
                motivos.Add($"Asistencia trimestral {alumno.AttendancePercentage!.Value:0.##}%");

            if (promedioBajo)
                motivos.Add($"Promedio trimestral {alumno.AverageGrade!.Value:0.##}");

            if (tieneAusenciasConsecutivas)
                motivos.Add($"{alumno.ConsecutiveAbsences} ausencias consecutivas");

            if (tieneUltimaNotaBaja)
                motivos.Add($"Ultima nota baja: {alumno.LatestLowGrade!.Grade:0.##}");

            alumno.AcademicReasons = motivos;

            if (tieneAusenciasConsecutivas || (asistenciaBaja && promedioBajo))
            {
                alumno.AcademicStatusLevel = "critical";
                alumno.AcademicStatusLabel = "Requiere intervencion prioritaria";
                return;
            }

            if (asistenciaBaja || promedioBajo || tieneUltimaNotaBaja || alumno.IsWithoutCourse)
            {
                alumno.AcademicStatusLevel = "follow-up";
                alumno.AcademicStatusLabel = "Requiere seguimiento";
                return;
            }

            alumno.AcademicStatusLevel = "normal";
            alumno.AcademicStatusLabel = "Sin alertas academicas";
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
    }
}
