using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Curso.Queries.GetCursoById
{
    public class GetCursoByIdQuery : IGetCursoByIdQuery
    {
        private readonly IDataBaseService _db;

        public GetCursoByIdQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int userId,
            bool isAdmin,
            CancellationToken ct = default)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Id inválido");

            if (userId <= 0)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "Usuario no autenticado");

            var query = _db.Cursos
                .AsNoTracking()
                .Include(c => c.Horarios)
                .Include(c => c.Profesores)
                .Include(c => c.Matriculas)
                .Where(c => c.Id == cursoId);

            if (!isAdmin)
            {
                query = query.Where(c => c.Profesores.Any(p => p.ProfesorId == userId));
            }

            var curso = await query.FirstOrDefaultAsync(ct);

            if (curso == null)
            {
                return ResponseApiService.Response(
                    StatusCodes.Status404NotFound,
                    "Curso no encontrado o sin permisos para acceder");
            }

            var dto = new GetCursoByIdModel
            {
                Id = curso.Id,
                Nombre = curso.Nombre,
                Anio = curso.Anio,
                Descripcion = curso.Descripcion,
                ThemeIcon = curso.ThemeIcon,
                Estado = curso.Estado,
                CantidadProfesores = curso.Profesores.Count,
                CantidadAlumnos = curso.Matriculas.Count,
                Horarios = curso.Horarios
                    .OrderBy(h => h.Dia)
                    .ThenBy(h => h.HoraInicio)
                    .Select(h => new GetCursoHorarioModel
                    {
                        Dia = (int)h.Dia,
                        HoraInicio = h.HoraInicio.ToString("HH:mm"),
                        HoraFin = h.HoraFin.ToString("HH:mm"),
                    })
                    .ToList()
            };

            return ResponseApiService.Response(StatusCodes.Status200OK, dto);
        }
    }
}
