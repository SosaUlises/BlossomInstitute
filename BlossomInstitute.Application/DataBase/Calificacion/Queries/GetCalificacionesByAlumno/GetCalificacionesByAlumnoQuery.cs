using BlossomInstitute.Application.DataBase.Calificacion.Queries.Model;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Calificaciones;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Calificacion.Queries.GetCalificacionesByAlumno
{
    public class GetCalificacionesByAlumnoQuery : IGetCalificacionesByAlumnoQuery
    {
        private readonly IDataBaseService _db;

        public GetCalificacionesByAlumnoQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(
            int alumnoId,
            int userId,
            bool isAdmin,
            bool isProfesor,
            int? cursoId,
            int pageNumber,
            int pageSize,
            CancellationToken ct)
        {
            if (alumnoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "AlumnoId inválido");

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            if (!isAdmin && !isProfesor && alumnoId != userId)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No autorizado");

            var alumnoExiste = await _db.Alumnos
                .AsNoTracking()
                .AnyAsync(x => x.Id == alumnoId, ct);

            if (!alumnoExiste)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Alumno no encontrado");

            IQueryable<CalificacionEntity> q = _db.Calificaciones
                .AsNoTracking()
                .Where(x => x.AlumnoId == alumnoId && !x.Archivado);

            if (isAdmin)
            {
                if (cursoId.HasValue)
                    q = q.Where(x => x.CursoId == cursoId.Value);
            }
            else if (isProfesor)
            {
                if (cursoId.HasValue)
                {
                    var profesorAsignado = await _db.CursoProfesores
                        .AsNoTracking()
                        .AnyAsync(x => x.CursoId == cursoId.Value && x.ProfesorId == userId, ct);

                    if (!profesorAsignado)
                        return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No autorizado para ver calificaciones de este curso");

                    q = q.Where(x => x.CursoId == cursoId.Value);
                }
                else
                {
                    var cursoIdsProfesor = await _db.CursoProfesores
                        .AsNoTracking()
                        .Where(x => x.ProfesorId == userId)
                        .Select(x => x.CursoId)
                        .Distinct()
                        .ToListAsync(ct);

                    if (cursoIdsProfesor.Count == 0)
                    {
                        return ResponseApiService.Response(StatusCodes.Status200OK, new
                        {
                            pageNumber,
                            pageSize,
                            total = 0,
                            items = new List<CalificacionListItemModel>()
                        });
                    }

                    q = q.Where(x => cursoIdsProfesor.Contains(x.CursoId));
                }
            }
            else
            {
                if (cursoId.HasValue)
                    q = q.Where(x => x.CursoId == cursoId.Value);
            }

            var total = await q.CountAsync(ct);

            var items = await q
                .OrderByDescending(x => x.Fecha)
                .ThenBy(x => x.Titulo)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CalificacionListItemModel
                {
                    Id = x.Id,
                    CursoId = x.CursoId,
                    CursoNombre = x.Curso.Nombre,
                    AlumnoId = x.AlumnoId,
                    AlumnoNombre = x.Alumno.Usuario.Nombre,
                    AlumnoApellido = x.Alumno.Usuario.Apellido,
                    AlumnoDni = x.Alumno.Usuario.Dni,
                    Tipo = x.Tipo,
                    Titulo = x.Titulo,
                    Descripcion = x.Descripcion,
                    Nota = x.Nota,
                    Fecha = x.Fecha,
                    TareaId = x.TareaId,
                    EntregaId = x.EntregaId,
                    TieneDetalleSkills = x.TieneDetalleSkills,
                    Detalles = x.Detalles
                        .OrderBy(d => d.Skill)
                        .Select(d => new CalificacionListDetalleItemModel
                        {
                            Skill = d.Skill,
                            PuntajeObtenido = d.PuntajeObtenido,
                            PuntajeMaximo = d.PuntajeMaximo,
                            Porcentaje = d.PuntajeMaximo > 0
                                ? Math.Round(d.PuntajeObtenido * 100 / d.PuntajeMaximo, 2)
                                : null
                        })
                        .ToList()
                })
                .ToListAsync(ct);

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
