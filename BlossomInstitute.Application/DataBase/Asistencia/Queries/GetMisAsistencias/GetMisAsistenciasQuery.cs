using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Asistencia.Queries.GetMisAsistencias
{
    public class GetMisAsistenciasQuery : IGetMisAsistenciasQuery
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public GetMisAsistenciasQuery(
            IDataBaseService db,
            UserManager<UsuarioEntity> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(
            int userId,
            int? cursoId,
            DateOnly? fechaDesde,
            DateOnly? fechaHasta,
            int pageNumber,
            int pageSize,
            CancellationToken ct = default)
        {
            if (userId <= 0)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "No autenticado");

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || !user.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Usuario invalido o inactivo");

            if (!await _userManager.IsInRoleAsync(user, "Alumno"))
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Acceso denegado");

            var alumnoId = userId;

            if (cursoId.HasValue && cursoId.Value <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "CursoId invalido");

            if (fechaDesde.HasValue && fechaHasta.HasValue && fechaDesde.Value > fechaHasta.Value)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "El rango de fechas es invalido (from > to)");

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 200) pageSize = 200;

            var matriculasQuery = _db.Matriculas.AsNoTracking()
                .Where(m => m.AlumnoId == alumnoId);

            if (cursoId.HasValue)
                matriculasQuery = matriculasQuery.Where(m => m.CursoId == cursoId.Value);

            var cursoIds = await matriculasQuery.Select(m => m.CursoId).Distinct().ToListAsync(ct);

            if (cursoIds.Count == 0)
            {
                return ResponseApiService.Response(StatusCodes.Status200OK, new MisAsistenciasResponseModel
                {
                    Total = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Items = new List<MisAsistenciasItemModel>()
                });
            }

            var clasesQuery = _db.Clases.AsNoTracking()
                .Where(c => cursoIds.Contains(c.CursoId));

            if (fechaDesde.HasValue) clasesQuery = clasesQuery.Where(c => c.Fecha >= fechaDesde.Value);
            if (fechaHasta.HasValue) clasesQuery = clasesQuery.Where(c => c.Fecha <= fechaHasta.Value);

            var total = await clasesQuery.CountAsync(ct);

            var clases = await clasesQuery
                .OrderByDescending(c => c.Fecha)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Id,
                    c.CursoId,
                    c.Fecha,
                    c.Estado,
                    c.Descripcion
                })
                .ToListAsync(ct);

            var claseIds = clases.Select(x => x.Id).ToList();

            var asistencias = await _db.Asistencias.AsNoTracking()
                .Where(a => a.AlumnoId == alumnoId && claseIds.Contains(a.ClaseId))
                .ToListAsync(ct);

            var asistenciasPorClaseId = asistencias.ToDictionary(x => x.ClaseId, x => x);

            var cursos = await _db.Cursos.AsNoTracking()
                .Where(c => cursoIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Nombre })
                .ToListAsync(ct);

            var cursoNombreById = cursos.ToDictionary(x => x.Id, x => x.Nombre);

            var items = clases.Select(c =>
            {
                asistenciasPorClaseId.TryGetValue(c.Id, out var registroAsistencia);
                cursoNombreById.TryGetValue(c.CursoId, out var nombreCurso);

                return new MisAsistenciasItemModel
                {
                    CursoId = c.CursoId,
                    CursoNombre = nombreCurso ?? $"Curso {c.CursoId}",
                    ClaseId = c.Id,
                    Fecha = c.Fecha.ToString("yyyy-MM-dd"),
                    EstadoClase = c.Estado,
                    Estado = registroAsistencia?.Estado,
                    DescripcionClase = c.Descripcion
                };
            }).ToList();

            return ResponseApiService.Response(StatusCodes.Status200OK, new MisAsistenciasResponseModel
            {
                Total = total,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Items = items
            });
        }
    }
}
