using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Entrega;
using BlossomInstitute.Domain.Entidades.Tarea;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Entregas.Commands.UpsertEntregaAlumno
{
    public class UpsertEntregaAlumnoCommand : IUpsertEntregaAlumnoCommand
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public UpsertEntregaAlumnoCommand(IDataBaseService db, UserManager<UsuarioEntity> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(int tareaId, int userId, UpsertEntregaAlumnoModel model, CancellationToken ct)
        {
            if (tareaId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "TareaId inválido");

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "No autenticado");

            if (!user.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Usuario inactivo");

            if (!await _userManager.IsInRoleAsync(user, "Alumno"))
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No autorizado");

            var alumnoId = user.Id;

            var tarea = await _db.Tareas
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tareaId, ct);

            if (tarea == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Tarea no encontrada");

            if (tarea.Estado == EstadoTarea.Archivada)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Tarea archivada");

            if (tarea.Estado != EstadoTarea.Publicada)
                return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "La tarea no está publicada");

            if (!tarea.FechaEntregaUtc.HasValue)
                return ResponseApiService.Response(
                    StatusCodes.Status409Conflict,
                    "Esta publicación es un anuncio y no admite entregas");

            var estaMatriculado = await _db.Matriculas
                .AsNoTracking()
                .AnyAsync(m => m.CursoId == tarea.CursoId && m.AlumnoId == alumnoId, ct);

            if (!estaMatriculado)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No estás matriculado en este curso");

            var nowUtc = DateTime.UtcNow;
            var deadlineUtc = tarea.FechaEntregaUtc;

            var estadoEntrega = (deadlineUtc.HasValue && nowUtc > deadlineUtc.Value)
                ? EstadoEntrega.FueraDeTermino
                : EstadoEntrega.EntregadaEnTermino;

            var adjuntos = model.Adjuntos ?? new List<UpsertEntregaAdjuntoModel>();

            adjuntos = adjuntos
                .Where(a =>
                    a.Tipo == TipoAdjunto.Link
                        ? !string.IsNullOrWhiteSpace(a.Url)
                        : !string.IsNullOrWhiteSpace(a.Url) || !string.IsNullOrWhiteSpace(a.StorageKey))
                .GroupBy(a =>
                    a.Tipo == TipoAdjunto.Archivo
                        ? (a.StorageKey ?? a.Url)!.Trim().ToLowerInvariant()
                        : a.Url!.Trim().ToLowerInvariant())
                .Select(g => g.First())
                .ToList();

            await using var tx = await _db.BeginTransactionAsync(ct);

            try
            {
                var entrega = await _db.Entregas
                    .Include(e => e.Adjuntos)
                    .FirstOrDefaultAsync(e => e.TareaId == tareaId && e.AlumnoId == alumnoId, ct);

                var created = false;

                if (entrega == null)
                {
                    created = true;

                    entrega = new EntregaEntity
                    {
                        TareaId = tareaId,
                        AlumnoId = alumnoId,
                        Texto = string.IsNullOrWhiteSpace(model.Texto) ? null : model.Texto.Trim(),
                        FechaEntregaUtc = nowUtc,
                        Estado = estadoEntrega,
                        CreatedAtUtc = nowUtc
                    };

                    _db.Entregas.Add(entrega);

                    var ok = await _db.SaveAsync(ct);
                    if (!ok)
                    {
                        await tx.RollbackAsync(ct);
                        return ResponseApiService.Response(StatusCodes.Status500InternalServerError, message: "No se pudo registrar la entrega");
                    }

                    foreach (var a in adjuntos)
                    {
                        _db.EntregaAdjuntos.Add(new EntregaAdjuntoEntity
                        {
                            EntregaId = entrega.Id,
                            Tipo = a.Tipo,
                            Url = (a.Url ?? a.StorageKey)!.Trim(),
                            Nombre = string.IsNullOrWhiteSpace(a.Nombre) ? null : a.Nombre.Trim(),
                            StorageProvider = a.Tipo == TipoAdjunto.Archivo ? a.StorageProvider : null,
                            StorageKey = a.Tipo == TipoAdjunto.Archivo ? a.StorageKey?.Trim() : null,
                            ContentType = a.Tipo == TipoAdjunto.Archivo ? a.ContentType?.Trim() : null,
                            SizeBytes = a.Tipo == TipoAdjunto.Archivo ? a.SizeBytes : null,
                            CreatedAtUtc = nowUtc
                        });
                    }

                    if (adjuntos.Count > 0)
                    {
                        var okAdjuntos = await _db.SaveAsync(ct);
                        if (!okAdjuntos)
                        {
                            await tx.RollbackAsync(ct);
                            return ResponseApiService.Response(StatusCodes.Status500InternalServerError, message: "No se pudieron guardar los adjuntos de la entrega");
                        }
                    }
                }
                else
                {
                    entrega.Texto = string.IsNullOrWhiteSpace(model.Texto) ? null : model.Texto.Trim();
                    entrega.FechaEntregaUtc = nowUtc;
                    entrega.Estado = estadoEntrega;
                    entrega.UpdatedAtUtc = nowUtc;

                    if (entrega.Adjuntos.Count > 0)
                        _db.EntregaAdjuntos.RemoveRange(entrega.Adjuntos);

                    foreach (var a in adjuntos)
                    {
                        _db.EntregaAdjuntos.Add(new EntregaAdjuntoEntity
                        {
                            EntregaId = entrega.Id,
                            Tipo = a.Tipo,
                            Url = (a.Url ?? a.StorageKey)!.Trim(),
                            Nombre = string.IsNullOrWhiteSpace(a.Nombre) ? null : a.Nombre.Trim(),
                            StorageProvider = a.Tipo == TipoAdjunto.Archivo ? a.StorageProvider : null,
                            StorageKey = a.Tipo == TipoAdjunto.Archivo ? a.StorageKey?.Trim() : null,
                            ContentType = a.Tipo == TipoAdjunto.Archivo ? a.ContentType?.Trim() : null,
                            SizeBytes = a.Tipo == TipoAdjunto.Archivo ? a.SizeBytes : null,
                            CreatedAtUtc = nowUtc
                        });
                    }

                    // Reentrega => deja sin vigencia el feedback anterior
                    await _db.EntregaFeedbacks
                        .Where(f => f.EntregaId == entrega.Id && f.EsVigente)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.EsVigente, false), ct);

                    // Reentrega => archiva la calificación homework activa ligada a esa entrega
                    await _db.Calificaciones
                        .Where(c =>
                            c.TareaId == tareaId &&
                            c.EntregaId == entrega.Id &&
                            !c.Archivado)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(x => x.Archivado, true)
                            .SetProperty(x => x.ArchivadoPorTarea, false)
                            .SetProperty(x => x.UpdatedAtUtc, nowUtc), ct);

                    var saved = await _db.SaveAsync(ct);
                    if (!saved)
                    {
                        await tx.RollbackAsync(ct);
                        return ResponseApiService.Response(StatusCodes.Status500InternalServerError, message: "No se pudo guardar la entrega");
                    }
                }

                await tx.CommitAsync(ct);

                return ResponseApiService.Response(
                    created ? StatusCodes.Status201Created : StatusCodes.Status200OK,
                    new
                    {
                        entrega.Id,
                        entrega.TareaId,
                        entrega.AlumnoId,
                        entrega.Estado,
                        entrega.FechaEntregaUtc,
                        created
                    },
                    created ? "Entrega creada correctamente" : "Entrega actualizada correctamente"
                );
            }
            catch (DbUpdateException ex)
            {
                await tx.RollbackAsync(ct);

                return ResponseApiService.Response(
                    StatusCodes.Status409Conflict,
                    ex.Message,
                    "Conflicto al guardar la entrega. Intentá nuevamente."
                );
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
    }
}
