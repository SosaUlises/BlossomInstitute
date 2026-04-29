using BlossomInstitute.Application.DataBase.Tarea.Queries.GetTareasById;
using BlossomInstitute.Application.DataBase.Tarea.Queries.Models;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Tarea.Queries.GetTareasById
{
    public class GetTareaByIdQuery : IGetTareaByIdQuery
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public GetTareaByIdQuery(
            IDataBaseService db,
            UserManager<UsuarioEntity> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int tareaId,
            int userId,
            CancellationToken ct = default)
        {
            if (cursoId <= 0 || tareaId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Parámetros inválidos");

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "No autenticado");

            if (!user.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Usuario inválido o inactivo");

            var isProfesor = await _userManager.IsInRoleAsync(user, "Profesor");
            var isAdmin = await _userManager.IsInRoleAsync(user, "Administrador");

            if (!isProfesor && !isAdmin)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Acceso denegado");

            if (!isAdmin)
            {
                var profesorAsignado = await _db.CursoProfesores
                    .AsNoTracking()
                    .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == userId, ct);

                if (!profesorAsignado)
                    return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No estás asignado a este curso");
            }

            var tarea = await _db.Tareas
                .AsNoTracking()
                .Where(t => t.Id == tareaId && t.CursoId == cursoId)
                .Select(t => new TareaDetailModel
                {
                    Id = t.Id,
                    CursoId = t.CursoId,
                    ProfesorId = t.ProfesorId,
                    Titulo = t.Titulo,
                    Consigna = t.Consigna,
                    Estado = (int)t.Estado,
                    FechaEntregaUtc = t.FechaEntregaUtc,
                    EsAnuncio = !t.FechaEntregaUtc.HasValue,
                    CreatedAtUtc = t.CreatedAtUtc,
                    UpdatedAtUtc = t.UpdatedAtUtc,
                    Recursos = t.Recursos
                        .OrderBy(r => r.Id)
                        .Select(r => new TareaRecursoItemModel
                        {
                            Id = r.Id,
                            Tipo = (int)r.Tipo,
                            Url = r.Url,
                            Nombre = r.Nombre,
                            StorageProvider = r.StorageProvider,
                            StorageKey = r.StorageKey,
                            ContentType = r.ContentType,
                            SizeBytes = r.SizeBytes
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(ct);

            if (tarea == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Tarea no encontrada");

            return ResponseApiService.Response(StatusCodes.Status200OK, tarea);
        }
    }
}
