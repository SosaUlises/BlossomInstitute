using BlossomInstitute.Application.DataBase.Tarea.Queries.GetTareasByCurso;
using BlossomInstitute.Application.DataBase.Tarea.Queries.Models;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Tarea;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

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
                    DueDateUtc = t.FechaEntregaUtc,
                    EsAnuncio = !t.FechaEntregaUtc.HasValue,
                    CreatedAtUtc = t.CreatedAtUtc,
                    UpdatedAtUtc = t.UpdatedAtUtc,
                    PublicationType = t.FechaEntregaUtc.HasValue ? "task" : "announcement",
                    AuthorName = t.Profesor.Usuario.Nombre + " " + t.Profesor.Usuario.Apellido,
                    AuthorAvatarUrl = t.Profesor.Usuario.AvatarUrl,
                    ContentPreview = t.Consigna,
                    ResourcesCount = t.Recursos.Count(),
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
                        .ToList(),
                    SubmissionsCount = _db.Entregas.Count(e => e.TareaId == t.Id),
                    PendingReviewsCount = _db.Entregas.Count(e =>
                        e.TareaId == t.Id &&
                        !e.Feedbacks.Any(f => f.EsVigente))
                })
                .ToListAsync(ct);

            foreach (var item in items)
            {
                item.AuthorName = NormalizeText(item.AuthorName);
                item.ContentPreview = BuildContentPreview(item.ContentPreview);
                item.ResourceSummary = BuildResourceSummary(item.ResourcesCount);
            }

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                pageNumber,
                pageSize,
                total,
                items
            });
        }

        private static string? BuildContentPreview(string? value)
        {
            var normalized = NormalizeText(Regex.Replace(value ?? string.Empty, "<[^>]+>", " "));

            if (string.IsNullOrWhiteSpace(normalized))
                return null;

            const int maxLength = 220;

            if (normalized.Length <= maxLength)
                return normalized;

            return normalized[..maxLength].TrimEnd() + "...";
        }

        private static string? BuildResourceSummary(int resourcesCount)
        {
            if (resourcesCount <= 0)
                return null;

            return resourcesCount == 1
                ? "1 recurso adjunto"
                : $"{resourcesCount} recursos adjuntos";
        }

        private static string? NormalizeText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return Regex.Replace(value, "\\s+", " ").Trim();
        }
    }
}

