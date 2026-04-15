using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Calificacion;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Command.CreatePlantilla
{
    public class CreatePlantillaCalificacionCommand : ICreatePlantillaCalificacionCommand
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public CreatePlantillaCalificacionCommand(
            IDataBaseService db,
            UserManager<UsuarioEntity> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int profesorUserId,
            CreatePlantillaCalificacionModel model,
            CancellationToken ct)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    "Curso inválido");

            if (model == null)
                return ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    "Modelo inválido");

            var profesor = await _userManager.FindByIdAsync(profesorUserId.ToString());
            if (profesor == null)
                return ResponseApiService.Response(
                    StatusCodes.Status401Unauthorized,
                    "No autenticado");

            if (!profesor.Activo)
                return ResponseApiService.Response(
                    StatusCodes.Status403Forbidden,
                    "Usuario inactivo");

            if (!await _userManager.IsInRoleAsync(profesor, "Profesor"))
                return ResponseApiService.Response(
                    StatusCodes.Status403Forbidden,
                    "No autorizado");

            var cursoExiste = await _db.Cursos
                .AsNoTracking()
                .AnyAsync(c => c.Id == cursoId, ct);

            if (!cursoExiste)
                return ResponseApiService.Response(
                    StatusCodes.Status404NotFound,
                    "Curso no encontrado");

            var profesorAsignado = await _db.CursoProfesores
                .AsNoTracking()
                .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);

            if (!profesorAsignado)
                return ResponseApiService.Response(
                    StatusCodes.Status403Forbidden,
                    "Profesor no asignado a este curso");

            var detalles = model.Detalles?
                .Where(x => x != null)
                .ToList() ?? new List<CreatePlantillaCalificacionDetalleModel>();

            var validacion = ValidarReglasDeNegocio(model, detalles);
            if (validacion != null)
                return validacion;

            var tituloNormalizado = model.Titulo.Trim().ToLower();

            var existePlantillaActiva = await _db.PlantillaCalificaciones
                .AsNoTracking()
                .AnyAsync(x =>
                    x.CursoId == cursoId &&
                    !x.Archivada &&
                    x.Titulo.ToLower() == tituloNormalizado,
                    ct);

            if (existePlantillaActiva)
                return ResponseApiService.Response(
                    StatusCodes.Status409Conflict,
                    "Ya existe una plantilla activa con ese título en el curso");

            var plantilla = new PlantillaCalificacionEntity
            {
                CursoId = cursoId,
                ProfesorId = profesorUserId,
                Tipo = model.Tipo,
                Titulo = model.Titulo.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(model.Descripcion)
                    ? null
                    : model.Descripcion.Trim(),
                Activa = true,
                Archivada = false,
                CreatedAtUtc = DateTime.UtcNow,
                Detalles = detalles
                    .Select(d => new PlantillaCalificacionDetalleEntity
                    {
                        Skill = d.Skill,
                        PuntajeMaximo = d.PuntajeMaximo
                    })
                    .ToList()
            };

            await using var tx = await _db.BeginTransactionAsync(ct);

            try
            {
                _db.PlantillaCalificaciones.Add(plantilla);

                var ok = await _db.SaveAsync(ct);
                if (!ok)
                {
                    await tx.RollbackAsync(ct);
                    return ResponseApiService.Response(
                        StatusCodes.Status500InternalServerError,
                        "No se pudo guardar la plantilla");
                }

                await tx.CommitAsync(ct);

                return ResponseApiService.Response(
                    StatusCodes.Status201Created,
                    new
                    {
                        plantilla.Id,
                        plantilla.CursoId,
                        plantilla.ProfesorId,
                        plantilla.Tipo,
                        plantilla.Titulo,
                        plantilla.Descripcion,
                        plantilla.Activa,
                        plantilla.Archivada,
                        plantilla.CreatedAtUtc,
                        detalles = plantilla.Detalles.Select(d => new
                        {
                            d.Id,
                            d.Skill,
                            d.PuntajeMaximo
                        })
                    },
                    "Plantilla creada correctamente");
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync(ct);
                return ResponseApiService.Response(
                    StatusCodes.Status409Conflict,
                    "No se pudo guardar la plantilla por conflicto de datos");
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        private BaseResponseModel? ValidarReglasDeNegocio(
            CreatePlantillaCalificacionModel model,
            List<CreatePlantillaCalificacionDetalleModel> detalles)
        {
            if (model.Tipo != TipoCalificacion.Quiz && model.Tipo != TipoCalificacion.Test)
            {
                return ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    "Solo se pueden crear plantillas para Quiz o Test");
            }

            if (string.IsNullOrWhiteSpace(model.Titulo))
            {
                return ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    "El título es obligatorio");
            }

            if (model.Titulo.Trim().Length > 100)
            {
                return ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    "El título no puede superar los 100 caracteres");
            }

            if (!string.IsNullOrWhiteSpace(model.Descripcion) &&
                model.Descripcion.Trim().Length > 500)
            {
                return ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    "La descripción no puede superar los 500 caracteres");
            }

            if (detalles.Count == 0)
            {
                return ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    "La plantilla debe incluir al menos una skill");
            }

            var skillsDuplicadas = detalles
                .GroupBy(x => x.Skill)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (skillsDuplicadas.Count > 0)
            {
                return ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    "No se puede repetir la misma skill dentro de una plantilla");
            }

            if (detalles.Any(x => x.PuntajeMaximo <= 0))
            {
                return ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    "El puntaje máximo debe ser mayor a cero");
            }

            return null;
        }
    }
}


