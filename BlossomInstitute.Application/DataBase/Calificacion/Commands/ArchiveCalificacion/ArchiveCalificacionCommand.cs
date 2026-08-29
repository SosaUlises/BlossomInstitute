using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Calificacion.Commands.ArchiveCalificacion
{
    public class ArchiveCalificacionCommand : IArchiveCalificacionCommand
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public ArchiveCalificacionCommand(IDataBaseService db, UserManager<UsuarioEntity> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int alumnoId,
            int calificacionId,
            int profesorUserId,
            CancellationToken ct)
        {
            if (cursoId <= 0 || alumnoId <= 0 || calificacionId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Parametros invalidos");

            var profesor = await _userManager.FindByIdAsync(profesorUserId.ToString());
            if (profesor == null)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "No autenticado");

            if (!profesor.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Usuario inactivo");

            if (!await _userManager.IsInRoleAsync(profesor, "Profesor"))
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No autorizado");

            var profesorAsignado = await _db.CursoProfesores.AsNoTracking()
                .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);

            if (!profesorAsignado)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Profesor no asignado a este curso");

            var calificacion = await _db.Calificaciones
                .FirstOrDefaultAsync(x =>
                    x.Id == calificacionId &&
                    x.CursoId == cursoId &&
                    x.AlumnoId == alumnoId, ct);

            if (calificacion == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Calificacion no encontrada");

            if (calificacion.Archivado)
                return ResponseApiService.Response(StatusCodes.Status200OK, message: "Calificacion ya estaba archivada");

            calificacion.Archivado = true;
            calificacion.ArchivadoPorTarea = false;
            calificacion.UpdatedAtUtc = DateTime.UtcNow;

            try
            {
                var ok = await _db.SaveAsync(ct);
                if (!ok)
                    return ResponseApiService.Response(StatusCodes.Status500InternalServerError, message: "No se pudo archivar la calificacion");
            }
            catch (DbUpdateException)
            {
                return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "No se pudo archivar la calificacion por conflicto de datos");
            }

            return ResponseApiService.Response(StatusCodes.Status200OK, message: "Calificacion archivada correctamente");
        }
    }
}
