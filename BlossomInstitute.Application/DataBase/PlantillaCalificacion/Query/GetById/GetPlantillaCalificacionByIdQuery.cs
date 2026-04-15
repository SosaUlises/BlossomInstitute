using BlossomInstitute.Application.DataBase.PlantillaCalificacion.Query.Models;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Query.GetById
{
    public class GetPlantillaCalificacionByIdQuery : IGetPlantillaCalificacionByIdQuery
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public GetPlantillaCalificacionByIdQuery(
            IDataBaseService db,
            UserManager<UsuarioEntity> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int plantillaId,
            int profesorUserId,
            CancellationToken ct)
        {
            if (cursoId <= 0 || plantillaId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "Parámetros inválidos");

            var profesor = await _userManager.FindByIdAsync(profesorUserId.ToString());
            if (profesor == null)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, "No autenticado");

            if (!profesor.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Usuario inactivo");

            if (!await _userManager.IsInRoleAsync(profesor, "Profesor"))
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "No autorizado");

            var cursoExiste = await _db.Cursos
                .AsNoTracking()
                .AnyAsync(x => x.Id == cursoId, ct);

            if (!cursoExiste)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Curso no encontrado");

            var profesorAsignado = await _db.CursoProfesores
                .AsNoTracking()
                .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);

            if (!profesorAsignado)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Profesor no asignado a este curso");

            var item = await _db.PlantillaCalificaciones
                .AsNoTracking()
                .Where(x =>
                    x.Id == plantillaId &&
                    x.CursoId == cursoId &&
                    !x.Archivada)
                .Select(x => new PlantillaCalificacionDetailModel
                {
                    Id = x.Id,
                    CursoId = x.CursoId,
                    Tipo = x.Tipo,
                    Titulo = x.Titulo,
                    Descripcion = x.Descripcion,
                    TieneDetalleSkills = x.Detalles.Any(),
                    PuntajeMaximoTotal = x.Detalles.Any()
                        ? x.Detalles.Sum(d => d.PuntajeMaximo)
                        : null,
                    CreatedAtUtc = x.CreatedAtUtc,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    Detalles = x.Detalles
                        .OrderBy(d => d.Id)
                        .Select(d => new PlantillaCalificacionDetalleModel
                        {
                            Id = d.Id,
                            Skill = d.Skill,
                            PuntajeMaximo = d.PuntajeMaximo
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(ct);

            if (item == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Plantilla no encontrada");

            return ResponseApiService.Response(StatusCodes.Status200OK, item);
        }
    }
}


