using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Calificacion;
using BlossomInstitute.Domain.Entidades.Calificaciones;
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

            if (model.Nota.HasValue && (model.Nota.Value < 0 || model.Nota.Value > 100))
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "La nota debe estar entre 0 y 100");

            var profesorUser = await _userManager.FindByIdAsync(profesorUserId.ToString());
            if (profesorUser == null)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, "No autenticado");

            if (!profesorUser.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Usuario inactivo");

            if (!await _userManager.IsInRoleAsync(profesorUser, "Profesor"))
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "No autorizado");

            var tarea = await _db.Tareas
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tareaId && t.CursoId == cursoId, ct);

            if (tarea == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Tarea no encontrada");

            if (tarea.Estado != EstadoTarea.Publicada)
                return ResponseApiService.Response(StatusCodes.Status409Conflict, "La tarea no está publicada");

            if (!tarea.FechaEntregaUtc.HasValue)
                return ResponseApiService.Response(
                    StatusCodes.Status409Conflict,
                    "Esta publicación es un anuncio y no admite feedback");


            var profAsignado = await _db.CursoProfesores
                .AsNoTracking()
                .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);

            if (!profAsignado)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Profesor no asignado a este curso");

            var entrega = await _db.Entregas
                .FirstOrDefaultAsync(e => e.TareaId == tareaId && e.AlumnoId == alumnoId, ct);

            if (entrega == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "El alumno no tiene entrega para esta tarea");

            var nowUtc = DateTime.UtcNow;

            var adjuntos = model.Adjuntos ?? new List<CreateFeedbackEntregaAdjuntoModel>();

            adjuntos = adjuntos
                .Where(a => !string.IsNullOrWhiteSpace(a.Url))
                .GroupBy(a =>
                    a.Tipo == TipoAdjunto.Archivo
                        ? (a.StorageKey ?? a.Url)!.Trim().ToLowerInvariant()
                        : a.Url!.Trim().ToLowerInvariant())
                .Select(g => g.First())
                .ToList();

            await using var tx = await _db.BeginTransactionAsync(ct);

            try
            {
                await _db.EntregaFeedbacks
                    .Where(f => f.EntregaId == entrega.Id && f.EsVigente)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.EsVigente, false), ct);

                var feedback = new FeedbackEntregaEntity
                {
                    EntregaId = entrega.Id,
                    EsVigente = true,
                    Comentario = string.IsNullOrWhiteSpace(model.Comentario) ? null : model.Comentario.Trim(),
                    Estado = model.Estado,
                    Nota = model.Nota,
                    FechaCorreccionUtc = nowUtc
                };

                foreach (var a in adjuntos)
                {
                    feedback.Adjuntos.Add(new FeedbackEntregaAdjuntoEntity
                    {
                        Tipo = a.Tipo,
                        Url = a.Url!.Trim(),
                        Nombre = string.IsNullOrWhiteSpace(a.Nombre) ? null : a.Nombre.Trim(),
                        StorageProvider = a.Tipo == TipoAdjunto.Archivo ? a.StorageProvider : null,
                        StorageKey = a.Tipo == TipoAdjunto.Archivo ? a.StorageKey?.Trim() : null,
                        ContentType = a.Tipo == TipoAdjunto.Archivo ? a.ContentType?.Trim() : null,
                        SizeBytes = a.Tipo == TipoAdjunto.Archivo ? a.SizeBytes : null,
                        CreatedAtUtc = nowUtc
                    });
                }

                _db.EntregaFeedbacks.Add(feedback);
                entrega.UpdatedAtUtc = nowUtc;

                var calificacion = await _db.Calificaciones
                    .Include(c => c.Detalles)
                    .FirstOrDefaultAsync(c =>
                        c.CursoId == cursoId &&
                        c.AlumnoId == alumnoId &&
                        c.TareaId == tareaId &&
                        c.EntregaId == entrega.Id &&
                        !c.Archivado, ct);

                if (model.Nota.HasValue)
                {
                    if (calificacion == null)
                    {
                        calificacion = new CalificacionEntity
                        {
                            CursoId = cursoId,
                            AlumnoId = alumnoId,
                            Tipo = TipoCalificacion.Homework,
                            Titulo = tarea.Titulo,
                            Descripcion = "Calificación generada desde el último feedback vigente",
                            Nota = model.Nota.Value,
                            Fecha = DateOnly.FromDateTime(nowUtc),
                            TareaId = tareaId,
                            EntregaId = entrega.Id,
                            TieneDetalleSkills = false,
                            Archivado = false,
                            ArchivadoPorTarea = false,
                            CreatedAtUtc = nowUtc
                        };

                        _db.Calificaciones.Add(calificacion);
                    }
                    else
                    {
                        if (calificacion.Detalles.Any())
                            _db.CalificacionDetalles.RemoveRange(calificacion.Detalles);

                        calificacion.Tipo = TipoCalificacion.Homework;
                        calificacion.Titulo = tarea.Titulo;
                        calificacion.Descripcion = "Calificación actualizada desde el último feedback vigente";
                        calificacion.Nota = model.Nota.Value;
                        calificacion.Fecha = DateOnly.FromDateTime(nowUtc);
                        calificacion.TareaId = tareaId;
                        calificacion.EntregaId = entrega.Id;
                        calificacion.TieneDetalleSkills = false;
                        calificacion.Archivado = false;
                        calificacion.ArchivadoPorTarea = false;
                        calificacion.UpdatedAtUtc = nowUtc;
                    }
                }
                else
                {
                    if (calificacion != null)
                    {
                        if (calificacion.Detalles.Any())
                            _db.CalificacionDetalles.RemoveRange(calificacion.Detalles);

                        calificacion.Archivado = true;
                        calificacion.ArchivadoPorTarea = false;
                        calificacion.UpdatedAtUtc = nowUtc;
                    }
                }

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
                    feedback.FechaCorreccionUtc,
                    adjuntos = feedback.Adjuntos.Select(a => new
                    {
                        a.Id,
                        a.Tipo,
                        a.Url,
                        a.Nombre,
                        a.StorageProvider,
                        a.StorageKey,
                        a.ContentType,
                        a.SizeBytes
                    })
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

