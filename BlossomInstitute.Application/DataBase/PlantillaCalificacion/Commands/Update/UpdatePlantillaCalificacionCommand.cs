using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Calificacion;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Commands.Update
{
    public class UpdatePlantillaCalificacionCommand : IUpdatePlantillaCalificacionCommand
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public UpdatePlantillaCalificacionCommand(
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
            UpdatePlantillaCalificacionModel model,
            CancellationToken ct)
        {
            if (cursoId <= 0 || plantillaId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Parámetros inválidos");

            if (model == null)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Modelo inválido");

            var profesor = await _userManager.FindByIdAsync(profesorUserId.ToString());
            if (profesor == null)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "No autenticado");

            if (!profesor.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Usuario inactivo");

            if (!await _userManager.IsInRoleAsync(profesor, "Profesor"))
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No autorizado");

            var cursoExiste = await _db.Cursos
                .AsNoTracking()
                .AnyAsync(x => x.Id == cursoId, ct);

            if (!cursoExiste)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Curso no encontrado");

            var profesorAsignado = await _db.CursoProfesores
                .AsNoTracking()
                .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);

            if (!profesorAsignado)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Profesor no asignado a este curso");

            var detalles = model.Detalles?
                .Where(x => x != null)
                .ToList() ?? new List<UpdatePlantillaCalificacionDetalleModel>();

            var validacion = ValidarReglasDeNegocio(model, detalles);
            if (validacion != null)
                return validacion;

            var plantilla = await _db.PlantillaCalificaciones
                .Include(x => x.Detalles)
                .FirstOrDefaultAsync(x => x.Id == plantillaId && x.CursoId == cursoId, ct);

            if (plantilla == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Plantilla no encontrada");



            var tituloNormalizado = model.Titulo.Trim();

            var existeOtraConMismoTitulo = await _db.PlantillaCalificaciones
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Id != plantillaId &&
                    x.CursoId == cursoId &&
                    x.Titulo.ToLower() == tituloNormalizado.ToLower(),
                    ct);

            if (existeOtraConMismoTitulo)
                return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "Ya existe otra plantilla activa con el mismo título en este curso");

            plantilla.Tipo = model.Tipo;
            plantilla.Titulo = tituloNormalizado;
            plantilla.Descripcion = string.IsNullOrWhiteSpace(model.Descripcion)
                ? null
                : model.Descripcion.Trim();
            plantilla.UpdatedAtUtc = DateTime.UtcNow;

            plantilla.Detalles.Clear();

            foreach (var detalle in detalles)
            {
                plantilla.Detalles.Add(new PlantillaCalificacionDetalleEntity
                {
                    Skill = detalle.Skill,
                    PuntajeMaximo = detalle.PuntajeMaximo
                });
            }

            try
            {
                var ok = await _db.SaveAsync(ct);
                if (!ok)
                    return ResponseApiService.Response(StatusCodes.Status500InternalServerError, message: "No se pudo actualizar la plantilla");

                return ResponseApiService.Response(StatusCodes.Status200OK, new
                {
                    plantilla.Id,
                    plantilla.CursoId,
                    plantilla.Tipo,
                    plantilla.Titulo,
                    plantilla.Descripcion,
                    detalles = plantilla.Detalles.Select(d => new
                    {
                        d.Id,
                        d.Skill,
                        d.PuntajeMaximo
                    })
                }, "Plantilla actualizada correctamente");
            }
            catch (DbUpdateException)
            {
                return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "No se pudo actualizar la plantilla por conflicto de datos");
            }
        }

        private BaseResponseModel? ValidarReglasDeNegocio(
            UpdatePlantillaCalificacionModel model,
            List<UpdatePlantillaCalificacionDetalleModel> detalles)
        {
            if (string.IsNullOrWhiteSpace(model.Titulo))
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "El título es obligatorio");

            if (model.Titulo.Trim().Length > 100)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "El título no puede superar los 100 caracteres");

            if (!string.IsNullOrWhiteSpace(model.Descripcion) && model.Descripcion.Trim().Length > 500)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "La descripción no puede superar los 500 caracteres");

            if (model.Tipo != TipoCalificacion.Quiz && model.Tipo != TipoCalificacion.Test)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "La plantilla solo puede ser de tipo Quiz o Test");

            if (detalles.Count == 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "La plantilla debe incluir al menos una skill");

            var skillsDuplicadas = detalles
                .GroupBy(x => x.Skill)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (skillsDuplicadas.Count > 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "No se puede repetir la misma skill dentro de una misma plantilla");

            if (detalles.Any(x => x.PuntajeMaximo <= 0))
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "El puntaje máximo debe ser mayor a cero");

            return null;
        }
    }
}