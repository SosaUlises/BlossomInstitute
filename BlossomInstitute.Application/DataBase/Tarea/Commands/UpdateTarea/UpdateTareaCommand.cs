using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Tarea;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
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

        public async Task<BaseResponseModel> Execute(int cursoId, int tareaId, int profesorUserId, UpdateTareaModel model, CancellationToken ct = default)
        {
            if (cursoId <= 0) return ResponseApiService.Response(400, "CursoId inválido");
            if (tareaId <= 0) return ResponseApiService.Response(400, "TareaId inválido");
            if (profesorUserId <= 0) return ResponseApiService.Response(401, "No autenticado");

            var user = await _userManager.FindByIdAsync(profesorUserId.ToString());
            if (user == null || !user.Activo) return ResponseApiService.Response(403, "Usuario inválido o inactivo");
            if (!await _userManager.IsInRoleAsync(user, "Profesor")) return ResponseApiService.Response(403, "Acceso denegado");

            var profesorAsignado = await _db.CursoProfesores.AsNoTracking()
                .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);
            if (!profesorAsignado)
                return ResponseApiService.Response(403, "No estás asignado a este curso");

            var tarea = await _db.Tareas
                .Include(t => t.Recursos)
                .FirstOrDefaultAsync(t => t.Id == tareaId && t.CursoId == cursoId, ct);

            if (tarea == null)
                return ResponseApiService.Response(404, "Tarea no encontrada");

            await using var tx = await _db.BeginTransactionAsync(ct);

            tarea.Titulo = model.Titulo.Trim();
            tarea.Consigna = model.Consigna?.Trim();
            tarea.FechaEntregaUtc = model.FechaEntregaUtc;
            tarea.Estado = model.Estado;
            tarea.UpdatedAtUtc = DateTime.UtcNow;

            // Reemplazar recursos
            tarea.Recursos.Clear();
            if (model.Recursos != null && model.Recursos.Count > 0)
            {
                foreach (var r in model.Recursos)
                {
                    tarea.Recursos.Add(new TareaRecursoEntity
                    {
                        Tipo = r.Tipo,
                        Url = r.Url.Trim(),
                        Nombre = r.Nombre?.Trim()
                    });
                }
            }

            await _db.SaveAsync(ct);
            await tx.CommitAsync(ct);

            return ResponseApiService.Response(200, new
            {
                tarea.Id,
                tarea.CursoId,
                tarea.Titulo,
                tarea.Estado,
                tarea.FechaEntregaUtc
            }, "Tarea actualizada correctamente");
        }
    }
}
