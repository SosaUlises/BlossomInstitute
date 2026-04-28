using BlossomInstitute.Application.DataBase.Tarea.Queries.GetTareasByCurso;
using BlossomInstitute.Application.DataBase.Tarea.Queries.Models;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Tarea;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Tarea.Queries.GetTareasByCurso
{
    public class GetTareasByCursoQuery : IGetTareasByCursoQuery
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public GetTareasByCursoQuery(
            IDataBaseService db,
            UserManager<UsuarioEntity> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int userId,
            int pageNumber,
            int pageSize,
            string? search,
            EstadoTarea? estado,
            CancellationToken ct = default)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "CursoId inválido");

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "No autenticado");

            if (!user.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Usuario inválido o inactivo");

            var isProfesor = await _userManager.IsInRoleAsync(user, "Profesor");
            var isAdmin = await _userManager.IsInRoleAsync(user, "Administrador");

            if (!isProfesor && !isAdmin)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Acceso denegado");

            var curso = await _db.Cursos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == cursoId, ct);

            if (curso == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Curso no encontrado");

            if (!isAdmin)
            {
                var profesorAsignado = await _db.CursoProfesores
                    .AsNoTracking()
                    .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == userId, ct);

                if (!profesorAsignado)
                    return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No estás asignado a este curso");
            }

            search = search?.Trim();

            var query = _db.Tareas
                .AsNoTracking()
                .Where(t => t.CursoId == cursoId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLowerInvariant();
                query = query.Where(t =>
                    t.Titulo.ToLower().Contains(s) ||
                    (t.Consigna != null && t.Consigna.ToLower().Contains(s)));
            }

            if (estado.HasValue)
            {
                query = query.Where(t => t.Estado == estado.Value);
            }

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(t => t.CreatedAtUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TareaByCursoItemModel
                {
                    Id = t.Id,
                    CursoId = t.CursoId,
                    ProfesorId = t.ProfesorId,
                    Titulo = t.Titulo,
                    Estado = (int)t.Estado,
                    FechaEntregaUtc = t.FechaEntregaUtc,
                    EsAnuncio = !t.FechaEntregaUtc.HasValue,
                    CreatedAtUtc = t.CreatedAtUtc
                })
                .ToListAsync(ct);

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                pageNumber,
                pageSize,
                total,
                items
            });
        }
    }
}

