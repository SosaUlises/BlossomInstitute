using BlossomInstitute.Application.DataBase.Calificacion.Queries.Model;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Calificacion;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Calificacion.Queries.GetCalificacionesByCurso
{
    public class GetCalificacionesByCursoQuery : IGetCalificacionesByCursoQuery
    {
        private readonly IDataBaseService _db;

        public GetCalificacionesByCursoQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int profesorUserId,
            bool isAdmin,
            int pageNumber,
            int pageSize,
            string? search,
            int? alumnoId,
            int? tipo,
            DateOnly? from,
            DateOnly? to,
            int? year,
            int? term,
            CancellationToken ct)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "CursoId inválido");

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            if (tipo.HasValue && !Enum.IsDefined(typeof(TipoCalificacion), tipo.Value))
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Tipo de calificación inválido");

            if (year.HasValue && (year.Value < 2000 || year.Value > 2100))
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Year inválido");

            if (term.HasValue && (term.Value < 1 || term.Value > 3))
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Term inválido. Valores permitidos: 1, 2 o 3.");

            if (term.HasValue && !year.HasValue)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Si se informa term, también debe informarse year.");

            var cursoExiste = await _db.Cursos
                .AsNoTracking()
                .AnyAsync(x => x.Id == cursoId, ct);

            if (!cursoExiste)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Curso no encontrado");

            if (!isAdmin)
            {
                var profesorAsignado = await _db.CursoProfesores
                    .AsNoTracking()
                    .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);

                if (!profesorAsignado)
                    return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Profesor no asignado a este curso");
            }

            // Resolver rango final de fechas
            DateOnly? finalFrom = from;
            DateOnly? finalTo = to;

            if (year.HasValue && term.HasValue)
            {
                var (termFrom, termTo) = GetTermRange(year.Value, term.Value);

                finalFrom = finalFrom.HasValue
                    ? (finalFrom.Value > termFrom ? finalFrom.Value : termFrom)
                    : termFrom;

                finalTo = finalTo.HasValue
                    ? (finalTo.Value < termTo ? finalTo.Value : termTo)
                    : termTo;
            }

            if (finalFrom.HasValue && finalTo.HasValue && finalTo.Value < finalFrom.Value)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Rango de fechas inválido");

            var q = _db.Calificaciones
                .AsNoTracking()
                .Where(x => x.CursoId == cursoId && !x.Archivado);

            if (alumnoId.HasValue)
                q = q.Where(x => x.AlumnoId == alumnoId.Value);

            if (tipo.HasValue)
            {
                var tipoEnum = (TipoCalificacion)tipo.Value;
                q = q.Where(x => x.Tipo == tipoEnum);
            }

            if (finalFrom.HasValue)
                q = q.Where(x => x.Fecha >= finalFrom.Value);

            if (finalTo.HasValue)
                q = q.Where(x => x.Fecha <= finalTo.Value);

            var projected = q.Select(x => new
            {
                x.Id,
                x.CursoId,
                x.AlumnoId,
                AlumnoNombre = x.Alumno.Usuario.Nombre,
                AlumnoApellido = x.Alumno.Usuario.Apellido,
                AlumnoDni = x.Alumno.Usuario.Dni,
                AlumnoEmail = x.Alumno.Usuario.Email,
                x.Tipo,
                x.Titulo,
                x.Descripcion,
                x.Nota,
                x.Fecha,
                x.TareaId,
                x.EntregaId,
                TieneDetalleSkills = x.TieneDetalleSkills
            });

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();

                projected = projected.Where(x =>
                    (x.AlumnoApellido ?? "").ToLower().Contains(s) ||
                    (x.AlumnoNombre ?? "").ToLower().Contains(s) ||
                    (x.AlumnoEmail ?? "").ToLower().Contains(s) ||
                    x.AlumnoDni.ToString().Contains(s) ||
                    (x.Titulo ?? "").ToLower().Contains(s) ||
                    (x.Descripcion ?? "").ToLower().Contains(s));
            }

            var total = await projected.CountAsync(ct);

            var items = await projected
                .OrderByDescending(x => x.Fecha)
                .ThenBy(x => x.AlumnoApellido)
                .ThenBy(x => x.AlumnoNombre)
                .ThenBy(x => x.Titulo)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CalificacionListItemModel
                {
                    Id = x.Id,
                    CursoId = x.CursoId,
                    AlumnoId = x.AlumnoId,
                    AlumnoNombre = x.AlumnoNombre ?? "",
                    AlumnoApellido = x.AlumnoApellido ?? "",
                    AlumnoDni = x.AlumnoDni,
                    Tipo = x.Tipo,
                    Titulo = x.Titulo,
                    Descripcion = x.Descripcion,
                    Nota = x.Nota,
                    Fecha = x.Fecha,
                    TareaId = x.TareaId,
                    EntregaId = x.EntregaId
                })
                .ToListAsync(ct);

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                pageNumber,
                pageSize,
                total,
                filtros = new
                {
                    cursoId,
                    alumnoId,
                    tipo,
                    from = finalFrom,
                    to = finalTo,
                    year,
                    term
                },
                items
            });
        }

        private static (DateOnly from, DateOnly to) GetTermRange(int year, int term)
        {
            return term switch
            {
                1 => (new DateOnly(year, 3, 1), new DateOnly(year, 5, 31)),
                2 => (new DateOnly(year, 6, 1), new DateOnly(year, 8, 31)),
                3 => (new DateOnly(year, 9, 1), new DateOnly(year, 11, 30)),
                _ => throw new ArgumentOutOfRangeException(nameof(term))
            };
        }
    }
}
