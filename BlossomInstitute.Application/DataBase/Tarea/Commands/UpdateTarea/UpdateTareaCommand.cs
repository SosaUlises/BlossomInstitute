using BlossomInstitute.Application.DataBase;
using BlossomInstitute.Application.DataBase.Tarea.Commands.UpdateTarea;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Entidades.Tarea;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Tarea.Commands.UpdateTarea
{
    public class UpdateTareaCommand : IUpdateTareaCommand
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public UpdateTareaCommand(IDataBaseService db, UserManager<UsuarioEntity> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int tareaId,
            int profesorUserId,
            UpdateTareaModel model,
            CancellationToken ct = default)
        {
            if (cursoId <= 0 || tareaId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "Parámetros inválidos");

            if (profesorUserId <= 0)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, "No autenticado");

            var user = await _userManager.FindByIdAsync(profesorUserId.ToString());
            if (user == null)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, "No autenticado");

            if (!user.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Usuario inválido o inactivo");

            var isProfesor = await _userManager.IsInRoleAsync(user, "Profesor");
            var isAdmin = await _userManager.IsInRoleAsync(user, "Administrador");

            if (!isProfesor && !isAdmin)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Acceso denegado");

            var tarea = await _db.Tareas
                .Include(t => t.Recursos)
                .FirstOrDefaultAsync(t => t.Id == tareaId && t.CursoId == cursoId, ct);

            if (tarea == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Tarea no encontrada");

            if (!isAdmin)
            {
                var profesorAsignado = await _db.CursoProfesores
                    .AsNoTracking()
                    .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);

                if (!profesorAsignado)
                    return ResponseApiService.Response(StatusCodes.Status403Forbidden, "No estás asignado a este curso");
            }

            var recursos = model.Recursos ?? new List<UpdateTareaRecursoModel>();

            recursos = recursos
                .Where(r => !string.IsNullOrWhiteSpace(r.Url))
                .GroupBy(r =>
                    !string.IsNullOrWhiteSpace(r.StorageKey)
                        ? r.StorageKey!.Trim().ToLowerInvariant()
                        : r.Url!.Trim().ToLowerInvariant())
                .Select(g => g.First())
                .ToList();

            await using var tx = await _db.BeginTransactionAsync(ct);

            try
            {
                tarea.Titulo = model.Titulo.Trim();
                tarea.Consigna = string.IsNullOrWhiteSpace(model.Consigna) ? null : model.Consigna.Trim();
                tarea.FechaEntregaUtc = model.FechaEntregaUtc;
                tarea.Estado = model.Estado;
                tarea.UpdatedAtUtc = DateTime.UtcNow;

                tarea.Recursos.Clear();

                foreach (var r in recursos)
                {
                    tarea.Recursos.Add(new TareaRecursoEntity
                    {
                        Tipo = r.Tipo,
                        Url = r.Url!.Trim(),
                        Nombre = string.IsNullOrWhiteSpace(r.Nombre) ? null : r.Nombre.Trim(),
                        StorageProvider = r.StorageProvider,
                        StorageKey = string.IsNullOrWhiteSpace(r.StorageKey) ? null : r.StorageKey.Trim(),
                        ContentType = string.IsNullOrWhiteSpace(r.ContentType) ? null : r.ContentType.Trim(),
                        SizeBytes = r.SizeBytes
                    });
                }

                var ok = await _db.SaveAsync(ct);
                if (!ok)
                {
                    await tx.RollbackAsync(ct);
                    return ResponseApiService.Response(StatusCodes.Status500InternalServerError, "No se pudo actualizar la tarea");
                }

                await tx.CommitAsync(ct);

                return ResponseApiService.Response(StatusCodes.Status200OK, new
                {
                    tarea.Id,
                    tarea.CursoId,
                    tarea.ProfesorId,
                    tarea.Titulo,
                    tarea.Estado,
                    tarea.FechaEntregaUtc,
                    EsAnuncio = !tarea.FechaEntregaUtc.HasValue
                }, "Tarea actualizada correctamente");
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
    }

    }


