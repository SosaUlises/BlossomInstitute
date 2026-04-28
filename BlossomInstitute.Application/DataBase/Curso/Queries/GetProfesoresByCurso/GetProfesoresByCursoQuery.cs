using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Curso.Queries.GetProfesoresByCurso
{
    public class GetProfesoresByCursoQuery : IGetProfesoresByCursoQuery
    {
        private readonly IDataBaseService _db;

        public GetProfesoresByCursoQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int userId,
            bool isAdmin,
            int pageNumber,
            int pageSize,
            string? search,
            CancellationToken ct)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "CursoId inválido");

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var cursoExiste = await _db.Cursos
                .AsNoTracking()
                .AnyAsync(x => x.Id == cursoId, ct);

            if (!cursoExiste)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Curso no encontrado");

            if (!isAdmin)
            {
                var profesorAsignado = await _db.CursoProfesores
                    .AsNoTracking()
                    .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == userId, ct);

                if (!profesorAsignado)
                    return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No autorizado");
            }

            var q = _db.CursoProfesores
                .AsNoTracking()
                .Where(x => x.CursoId == cursoId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                q = q.Where(x =>
                    x.Profesor.Usuario.Nombre.Contains(search) ||
                    x.Profesor.Usuario.Apellido.Contains(search) ||
                    x.Profesor.Usuario.Email!.Contains(search) ||
                    x.Profesor.Usuario.Dni.ToString().Contains(search));
            }

            var total = await q.CountAsync(ct);

            var items = await q
                .OrderBy(x => x.Profesor.Usuario.Apellido)
                .ThenBy(x => x.Profesor.Usuario.Nombre)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ProfesorByCursoItemModel
                {
                    ProfesorId = x.ProfesorId,
                    Nombre = x.Profesor.Usuario.Nombre,
                    Apellido = x.Profesor.Usuario.Apellido,
                    Dni = x.Profesor.Usuario.Dni,
                    Email = x.Profesor.Usuario.Email
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
