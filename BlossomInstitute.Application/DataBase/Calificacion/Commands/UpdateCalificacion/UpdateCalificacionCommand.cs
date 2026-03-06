using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Calificacion.Commands.UpdateCalificacion
{
    public class UpdateCalificacionCommand : IUpdateCalificacionCommand
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public UpdateCalificacionCommand(IDataBaseService db, UserManager<UsuarioEntity> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int alumnoId,
            int calificacionId,
            int profesorUserId,
            UpdateCalificacionModel model,
            CancellationToken ct)
        {
            if (cursoId <= 0 || alumnoId <= 0 || calificacionId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "Parámetros inválidos");

            var profesor = await _userManager.FindByIdAsync(profesorUserId.ToString());
            if (profesor == null)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, "No autenticado");

            if (!profesor.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Usuario inactivo");

            if (!await _userManager.IsInRoleAsync(profesor, "Profesor"))
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "No autorizado");

            var profesorAsignado = await _db.CursoProfesores.AsNoTracking()
                .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);

            if (!profesorAsignado)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Profesor no asignado a este curso");

            var calificacion = await _db.Calificaciones
                .FirstOrDefaultAsync(x =>
                    x.Id == calificacionId &&
                    x.CursoId == cursoId &&
                    x.AlumnoId == alumnoId &&
                    !x.Archivado, ct);

            if (calificacion == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Calificación no encontrada");

            if (model.TareaId.HasValue)
            {
                var tareaValida = await _db.Tareas.AsNoTracking()
                    .AnyAsync(t => t.Id == model.TareaId.Value && t.CursoId == cursoId, ct);

                if (!tareaValida)
                    return ResponseApiService.Response(StatusCodes.Status409Conflict, "La tarea no pertenece al curso");
            }

            if (model.EntregaId.HasValue)
            {
                var entregaValida = await _db.Entregas.AsNoTracking()
                    .AnyAsync(e => e.Id == model.EntregaId.Value && e.AlumnoId == alumnoId, ct);

                if (!entregaValida)
                    return ResponseApiService.Response(StatusCodes.Status409Conflict, "La entrega no corresponde al alumno");
            }

            await using var tx = await _db.BeginTransactionAsync(ct);

            try
            {
                calificacion.Tipo = model.Tipo;
                calificacion.Titulo = model.Titulo.Trim();
                calificacion.Descripcion = string.IsNullOrWhiteSpace(model.Descripcion) ? null : model.Descripcion.Trim();
                calificacion.Nota = model.Nota;
                calificacion.Fecha = model.Fecha;
                calificacion.TareaId = model.TareaId;
                calificacion.EntregaId = model.EntregaId;
                calificacion.UpdatedAtUtc = DateTime.UtcNow;

                var ok = await _db.SaveAsync(ct);
                if (!ok)
                {
                    await tx.RollbackAsync(ct);
                    return ResponseApiService.Response(StatusCodes.Status500InternalServerError, "No se pudo actualizar la calificación");
                }

                await tx.CommitAsync(ct);

                return ResponseApiService.Response(StatusCodes.Status200OK, new
                {
                    calificacion.Id,
                    calificacion.CursoId,
                    calificacion.AlumnoId,
                    calificacion.Titulo,
                    calificacion.Nota,
                    calificacion.Fecha
                }, "Calificación actualizada correctamente");
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
    }
}
