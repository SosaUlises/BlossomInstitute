using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Curso.Commands.UpdateCursoTheme
{
    public class UpdateCursoThemeCommand : IUpdateCursoThemeCommand
    {
        private readonly IDataBaseService _db;

        public UpdateCursoThemeCommand(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int userId,
            bool isAdmin,
            UpdateCursoThemeModel model,
            CancellationToken ct = default)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Id inválido");

            if (userId <= 0)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "Usuario no autenticado");

            var themeIcon = model.ThemeIcon?.Trim();

            if (themeIcon?.Length > 200)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "La configuración visual no puede superar los 200 caracteres");

            var query = _db.Cursos.Where(c => c.Id == cursoId);

            if (!isAdmin)
            {
                query = query.Where(c => c.Profesores.Any(p => p.ProfesorId == userId));
            }

            var curso = await query.FirstOrDefaultAsync(ct);

            if (curso == null)
            {
                return ResponseApiService.Response(
                    StatusCodes.Status404NotFound,
                    message: "Curso no encontrado o sin permisos para modificarlo");
            }

            curso.ThemeIcon = string.IsNullOrWhiteSpace(themeIcon) ? null : themeIcon;

            await _db.SaveAsync(ct);

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                curso.Id,
                curso.ThemeIcon
            }, "Portada del curso actualizada correctamente");
        }
    }
}
