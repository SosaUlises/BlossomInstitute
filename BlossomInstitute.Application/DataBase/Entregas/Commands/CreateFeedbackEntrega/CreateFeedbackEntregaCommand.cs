using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Entrega;
using BlossomInstitute.Domain.Entidades.Tarea;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Entregas.Commands.CreateFeedbackEntrega
{
    public class CreateFeedbackEntregaCommand : ICreateFeedbackEntregaCommand
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public CreateFeedbackEntregaCommand(IDataBaseService db, UserManager<UsuarioEntity> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int tareaId,
            int alumnoId,
            int profesorUserId,
            CreateFeedbackEntregaModel model,
            CancellationToken ct)
        {
            if (cursoId <= 0 || tareaId <= 0 || alumnoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "Parámetros inválidos");

            // Auth profesor
            var profesorUser = await _userManager.FindByIdAsync(profesorUserId.ToString());
            if (profesorUser == null)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, "No autenticado");

            if (!profesorUser.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Usuario inactivo");

            if (!await _userManager.IsInRoleAsync(profesorUser, "Profesor"))
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "No autorizado");

            // Tarea pertenece al curso + está publicada
            var tarea = await _db.Tareas
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tareaId && t.CursoId == cursoId, ct);

            if (tarea == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Tarea no encontrada");

            if (tarea.Estado != EstadoTarea.Publicada)
                return ResponseApiService.Response(StatusCodes.Status409Conflict, "La tarea no está publicada");

            // Validar profesor asignado al curso 
            var profAsignado = await _db.CursoProfesores
                .AsNoTracking()
                .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);

            if (!profAsignado)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Profesor no asignado a este curso");

            // Entrega existe
            var entrega = await _db.Entregas
                .FirstOrDefaultAsync(e => e.TareaId == tareaId && e.AlumnoId == alumnoId, ct);

            if (entrega == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "El alumno no tiene entrega para esta tarea");

            var nowUtc = DateTime.UtcNow;

            await using var tx = await _db.BeginTransactionAsync(ct);

            try
            {
                // Marcar feedback vigente anterior como no vigente (solo los vigentes)
                await _db.EntregaFeedbacks
                    .Where(f => f.EntregaId == entrega.Id && f.EsVigente)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.EsVigente, false), ct);

                // Crear nuevo feedback vigente
                var feedback = new FeedbackEntregaEntity
                {
                    EntregaId = entrega.Id,
                    EsVigente = true,
                    Comentario = string.IsNullOrWhiteSpace(model.Comentario) ? null : model.Comentario.Trim(),
                    Estado = model.Estado,
                    Nota = model.Nota,
                    FechaCorreccionUtc = nowUtc,
                    ArchivoCorregidoUrl = string.IsNullOrWhiteSpace(model.ArchivoCorregidoUrl) ? null : model.ArchivoCorregidoUrl.Trim(),
                    ArchivoCorregidoNombre = string.IsNullOrWhiteSpace(model.ArchivoCorregidoNombre) ? null : model.ArchivoCorregidoNombre.Trim(),
                };

                _db.EntregaFeedbacks.Add(feedback);
                entrega.UpdatedAtUtc = nowUtc;

                var ok = await _db.SaveAsync(ct);
                if (!ok)
                {
                    await tx.RollbackAsync(ct);
                    return ResponseApiService.Response(StatusCodes.Status500InternalServerError, "No se pudo guardar el feedback");
                }

                await tx.CommitAsync(ct);

                return ResponseApiService.Response(StatusCodes.Status201Created, new
                {
                    feedback.Id,
                    feedback.EntregaId,
                    feedback.Estado,
                    feedback.Nota,
                    feedback.EsVigente,
                    feedback.FechaCorreccionUtc
                }, "Feedback registrado correctamente");
            }
            catch (DbUpdateException ex)
            {
                await tx.RollbackAsync(ct);

                return ResponseApiService.Response(StatusCodes.Status409Conflict, ex.Message, "Conflicto al registrar feedback");
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
    }
}
