using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Tarea;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Tarea.Commands.ArchivarTarea
{
    public class ArchivarTareaCommand : IArchivarTareaCommand
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public ArchivarTareaCommand(IDataBaseService db, UserManager<UsuarioEntity> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(int cursoId, int tareaId, int profesorUserId, CancellationToken ct = default)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "CursoId inválido");

            if (tareaId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "TareaId inválido");

            var user = await _userManager.FindByIdAsync(profesorUserId.ToString());
            if (user == null || !user.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Usuario inválido o inactivo");

            if (!await _userManager.IsInRoleAsync(user, "Profesor"))
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Acceso denegado");

            var profesorAsignado = await _db.CursoProfesores.AsNoTracking()
                .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);

            if (!profesorAsignado)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No estás asignado a este curso");

            var tarea = await _db.Tareas
                .FirstOrDefaultAsync(t => t.Id == tareaId && t.CursoId == cursoId, ct);

            if (tarea == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Tarea no encontrada");

            if (tarea.Estado == EstadoTarea.Archivada)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "La tarea ya está archivada");

            var nowUtc = DateTime.UtcNow;

            await using var tx = await _db.BeginTransactionAsync(ct);

            try
            {
                tarea.Estado = EstadoTarea.Archivada;
                tarea.UpdatedAtUtc = nowUtc;

                // Archivar calificaciones asociadas a la tarea
                await _db.Calificaciones
                    .Where(c => c.TareaId == tareaId && !c.Archivado)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.Archivado, true)
                        .SetProperty(x => x.ArchivadoPorTarea, true)
                        .SetProperty(x => x.UpdatedAtUtc, nowUtc), ct);

                var ok = await _db.SaveAsync(ct);
                if (!ok)
                {
                    await tx.RollbackAsync(ct);
                    return ResponseApiService.Response(StatusCodes.Status500InternalServerError, message: "No se pudo archivar la tarea");
                }

                await tx.CommitAsync(ct);

                return ResponseApiService.Response(StatusCodes.Status200OK, message: "Tarea archivada correctamente");
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
    }
}
