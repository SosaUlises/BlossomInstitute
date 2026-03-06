using BlossomInstitute.Application.DataBase.Calificacion.Queries.Model;
using BlossomInstitute.Common.Features;
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
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "AlumnoId inválido");

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            // Si no es admin/profesor, solo puede ver las propias
            if (!isAdmin && !isProfesor && alumnoId != userId)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "No autorizado");

            var alumnoExiste = await _db.Alumnos.AsNoTracking()
                .AnyAsync(x => x.Id == alumnoId, ct);

            if (!alumnoExiste)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Alumno no encontrado");

            var q = _db.Calificaciones.AsNoTracking()
                .Where(x => x.AlumnoId == alumnoId && !x.Archivado);

            if (cursoId.HasValue)
                q = q.Where(x => x.CursoId == cursoId.Value);

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
                    EntregaId = x.EntregaId
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
