using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Curso.Queries.GetPersonasAlumnoCurso
{
    public class GetPersonasAlumnoCursoQuery : IGetPersonasAlumnoCursoQuery
    {
        private readonly IDataBaseService _db;

        public GetPersonasAlumnoCursoQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int cursoId, int alumnoUserId, CancellationToken ct = default)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "CursoId invÃ¡lido");

            if (alumnoUserId <= 0)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "No autenticado");

            var matriculado = await _db.Matriculas
                .AsNoTracking()
                .AnyAsync(m => m.CursoId == cursoId && m.AlumnoId == alumnoUserId, ct);

            if (!matriculado)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No estÃ¡s matriculado en este curso");

            var profesores = await _db.CursoProfesores
                .AsNoTracking()
                .Where(x => x.CursoId == cursoId)
                .OrderBy(x => x.Profesor.Usuario.Apellido)
                .ThenBy(x => x.Profesor.Usuario.Nombre)
                .Select(x => new
                {
                    x.ProfesorId,
                    x.Profesor.Usuario.Nombre,
                    x.Profesor.Usuario.Apellido,
                    x.Profesor.Usuario.AvatarUrl
                })
                .ToListAsync(ct);

            var companeros = await _db.Matriculas
                .AsNoTracking()
                .Where(x => x.CursoId == cursoId)
                .OrderBy(x => x.Alumno.Usuario.Apellido)
                .ThenBy(x => x.Alumno.Usuario.Nombre)
                .Select(x => new
                {
                    x.AlumnoId,
                    x.Alumno.Usuario.Nombre,
                    x.Alumno.Usuario.Apellido,
                    x.Alumno.Usuario.AvatarUrl
                })
                .ToListAsync(ct);

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                profesores,
                companeros
            });
        }
    }
}
