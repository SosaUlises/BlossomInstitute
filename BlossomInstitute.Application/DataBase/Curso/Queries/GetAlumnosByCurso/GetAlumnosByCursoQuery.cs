using BlossomInstitute.Application.Common.Academico;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Calificacion;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Curso.Queries.GetAlumnosByCurso
{
    public class GetAlumnosByCursoQuery : IGetAlumnosByCursoQuery
    {
        private readonly IDataBaseService _db;

        public GetAlumnosByCursoQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int userId,
            bool isAdmin,
            int pageNumber,
            int pageSize,
            string? search,
            CancellationToken ct)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "CursoId inválido");

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var curso = await _db.Cursos
                .AsNoTracking()
                .Where(x => x.Id == cursoId)
                .Select(x => new { x.Id, x.Anio })
                .FirstOrDefaultAsync(ct);

            if (curso is null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Curso no encontrado");

            if (!isAdmin)
            {
                var profesorAsignado = await _db.CursoProfesores
                    .AsNoTracking()
                    .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == userId, ct);

                if (!profesorAsignado)
                    return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No autorizado");
            }

            var q = _db.Matriculas
                .AsNoTracking()
                .Where(x => x.CursoId == cursoId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                q = q.Where(x =>
                    x.Alumno.Usuario.Nombre.Contains(search) ||
                    x.Alumno.Usuario.Apellido.Contains(search) ||
                    x.Alumno.Usuario.Email!.Contains(search) ||
                    x.Alumno.Usuario.Dni.ToString().Contains(search));
            }

            var total = await q.CountAsync(ct);

            var items = await q
                .OrderBy(x => x.Alumno.Usuario.Apellido)
                .ThenBy(x => x.Alumno.Usuario.Nombre)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AlumnoByCursoItemModel
                {
                    AlumnoId = x.AlumnoId,
                    Nombre = x.Alumno.Usuario.Nombre,
                    Apellido = x.Alumno.Usuario.Apellido,
                    Dni = x.Alumno.Usuario.Dni,
                    Email = x.Alumno.Usuario.Email,
                    AvatarUrl = x.Alumno.Usuario.AvatarUrl
                })
                .ToListAsync(ct);

            var alumnoIds = items.Select(x => x.AlumnoId).ToList();
            var quarters = Enumerable.Range(1, 3)
                .Select(quarter => PeriodoAcademicoHelper.ObtenerTrimestre(curso.Anio, quarter))
                .ToList();
            var yearFrom = quarters[0].Desde;
            var yearTo = quarters[^1].Hasta;

            var averages = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    x.CursoId == cursoId &&
                    alumnoIds.Contains(x.AlumnoId) &&
                    !x.Archivado &&
                    x.Fecha >= yearFrom &&
                    x.Fecha <= yearTo &&
                    (x.Tipo == TipoCalificacion.Quiz || x.Tipo == TipoCalificacion.Test))
                .Select(x => new { x.AlumnoId, x.Fecha, x.Nota })
                .ToListAsync(ct);

            var classes = await _db.Clases
                .AsNoTracking()
                .Where(x =>
                    x.CursoId == cursoId &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha >= yearFrom &&
                    x.Fecha <= yearTo)
                .Select(x => new { x.Id, x.Fecha })
                .ToListAsync(ct);

            var classIds = classes.Select(x => x.Id).ToList();
            var attendanceRows = await _db.Asistencias
                .AsNoTracking()
                .Where(x =>
                    classIds.Contains(x.ClaseId) &&
                    alumnoIds.Contains(x.AlumnoId))
                .Select(x => new { x.AlumnoId, x.ClaseId, x.Estado })
                .ToListAsync(ct);

            foreach (var item in items)
            {
                var studentGrades = averages.Where(x => x.AlumnoId == item.AlumnoId).ToList();

                item.PromediosTrimestrales = quarters
                    .Select(quarter =>
                    {
                        var grades = studentGrades
                            .Where(x => x.Fecha >= quarter.Desde && x.Fecha <= quarter.Hasta)
                            .Select(x => x.Nota)
                            .ToList();
                        var quarterClassIds = classes
                            .Where(x => x.Fecha >= quarter.Desde && x.Fecha <= quarter.Hasta)
                            .Select(x => x.Id)
                            .ToHashSet();
                        var presentCount = attendanceRows.Count(x =>
                            x.AlumnoId == item.AlumnoId &&
                            quarterClassIds.Contains(x.ClaseId) &&
                            x.Estado == EstadoAsistencia.Presente);

                        return new AlumnoByCursoQuarterAverageModel
                        {
                            Quarter = quarter.Trimestre,
                            Label = quarter.Etiqueta,
                            From = quarter.Desde,
                            To = quarter.Hasta,
                            Promedio = grades.Count > 0
                                ? Math.Round(grades.Average(), 2)
                                : null,
                            Asistencia = quarterClassIds.Count > 0
                                ? Math.Round((decimal)presentCount * 100 / quarterClassIds.Count, 2)
                                : null
                        };
                    })
                    .ToList();
            }

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                pageNumber,
                pageSize,
                total,
                items
            });
        }
    }
}
