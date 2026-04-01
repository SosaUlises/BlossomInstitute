using BlossomInstitute.Application.DataBase.Calificacion.Queries.Model;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Calificacion.Queries.GetCalificacionById
{
    public class GetCalificacionByIdQuery : IGetCalificacionByIdQuery
    {
        private readonly IDataBaseService _db;

        public GetCalificacionByIdQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int alumnoId,
            int calificacionId,
            int userId,
            bool isAdmin,
            bool isProfesor,
            CancellationToken ct)
        {
            if (cursoId <= 0 || alumnoId <= 0 || calificacionId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "Parámetros inválidos");

            if (!isAdmin && !isProfesor)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "No autorizado");

            if (isProfesor && !isAdmin)
            {
                var profesorAsignado = await _db.CursoProfesores
                    .AsNoTracking()
                    .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == userId, ct);

                if (!profesorAsignado)
                    return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Profesor no asignado a este curso");
            }

            var calificacion = await _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    x.Id == calificacionId &&
                    x.CursoId == cursoId &&
                    x.AlumnoId == alumnoId &&
                    !x.Archivado)
                .Select(x => new CalificacionDetailModel
                {
                    Id = x.Id,
                    CursoId = x.CursoId,
                    AlumnoId = x.AlumnoId,
                    Tipo = x.Tipo,
                    Titulo = x.Titulo,
                    Descripcion = x.Descripcion,
                    Nota = x.Nota,
                    Fecha = x.Fecha,
                    TareaId = x.TareaId,
                    EntregaId = x.EntregaId,
                    TieneDetalleSkills = x.TieneDetalleSkills,
                    Detalles = x.Detalles
                        .Select(d => new CalificacionDetalleItemModel
                        {
                            Skill = d.Skill,
                            PuntajeObtenido = d.PuntajeObtenido,
                            PuntajeMaximo = d.PuntajeMaximo
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(ct);

            if (calificacion == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Calificación no encontrada");

            return ResponseApiService.Response(StatusCodes.Status200OK, calificacion);
        }
    }
}
