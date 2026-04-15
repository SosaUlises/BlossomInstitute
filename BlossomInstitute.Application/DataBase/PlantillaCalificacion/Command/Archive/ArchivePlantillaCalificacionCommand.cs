using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Command.Archive
{
    public class ArchivePlantillaCalificacionCommand : IArchivePlantillaCalificacionCommand
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public ArchivePlantillaCalificacionCommand(
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

            var plantilla = await _db.PlantillaCalificaciones
                .FirstOrDefaultAsync(x => x.Id == plantillaId && x.CursoId == cursoId, ct);

            if (plantilla == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Plantilla no encontrada");

            if (plantilla.Archivada)
                return ResponseApiService.Response(StatusCodes.Status409Conflict, "La plantilla ya está archivada");

            plantilla.Archivada = true;
            plantilla.UpdatedAtUtc = DateTime.UtcNow;

            try
            {
                var ok = await _db.SaveAsync(ct);
                if (!ok)
                    return ResponseApiService.Response(StatusCodes.Status500InternalServerError, "No se pudo archivar la plantilla");

                return ResponseApiService.Response(StatusCodes.Status200OK, new
                {
                    plantilla.Id,
                    plantilla.CursoId,
                    plantilla.Archivada,
                }, "Plantilla archivada correctamente");
            }
            catch (DbUpdateException)
            {
                return ResponseApiService.Response(StatusCodes.Status409Conflict, "No se pudo archivar la plantilla por conflicto de datos");
            }
        }
    }
}

