using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Curso.Commands.AsignarAlumnos
{
    public class MatricularAlumnosCommand : IMatricularAlumnosCommand
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public MatricularAlumnosCommand(
            IDataBaseService db, 
            UserManager<UsuarioEntity> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(int cursoId, MatricularAlumnosModel model, CancellationToken ct = default)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Id de curso inválido");

            var curso = await _db.Cursos.FirstOrDefaultAsync(c => c.Id == cursoId, ct);
            if (curso == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Curso no encontrado");

            if (curso.Estado == EstadoCurso.Inactivo)
                return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "No se puede matricular en un curso inactivo");
            if (curso.Estado == EstadoCurso.Archivado)
                return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "No se puede matricular en un curso archivado");

            var alumnoIds = model.AlumnoIds.Distinct().ToList();

            var alumnos = await _db.Alumnos
                .Where(a => alumnoIds.Contains(a.Id))
                .Select(a => a.Id)
                .ToListAsync(ct);

            var notFound = alumnoIds.Except(alumnos).ToList();
            if (notFound.Count > 0)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, new { alumnoIdsNoEncontrados = notFound }, "Hay alumnos que no existen");

            // Validar rol Alumno
            foreach (var aid in alumnos)
            {
                var user = await _userManager.FindByIdAsync(aid.ToString());
                if (user == null || !user.Activo)
                    return ResponseApiService.Response(StatusCodes.Status409Conflict, message: $"El alumno {aid} está inactivo o no existe como usuario");

                if (!await _userManager.IsInRoleAsync(user, "Alumno"))
                    return ResponseApiService.Response(StatusCodes.Status409Conflict, message: $"El usuario {aid} no tiene rol Alumno");
            }

            var existentes = await _db.Matriculas
                .Where(m => m.CursoId == cursoId && alumnoIds.Contains(m.AlumnoId))
                .Select(m => m.AlumnoId)
                .ToListAsync(ct);

            var nuevos = alumnoIds.Except(existentes).ToList();

            await using var tx = await _db.BeginTransactionAsync(ct);

            foreach (var aid in nuevos)
            {
                _db.Matriculas.Add(new MatriculaEntity
                {
                    CursoId = cursoId,
                    AlumnoId = aid
                });
            }

            await _db.SaveAsync(ct);
            await tx.CommitAsync(ct);

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                cursoId,
                agregados = nuevos,
                yaExistian = existentes
            }, "Alumnos matriculados");
        }
    }
}
