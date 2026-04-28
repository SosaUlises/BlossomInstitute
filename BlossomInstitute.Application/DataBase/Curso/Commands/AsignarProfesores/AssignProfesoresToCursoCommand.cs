using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Curso.Commands.AsignarProfesores
{
    public class AssignProfesoresToCursoCommand : IAssignProfesoresToCursoCommand
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public AssignProfesoresToCursoCommand(
            IDataBaseService db,
            UserManager<UsuarioEntity> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(int cursoId, AssignProfesoresToCursoModel model, CancellationToken ct = default)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Id de curso inválido");

            var curso = await _db.Cursos.FirstOrDefaultAsync(c => c.Id == cursoId, ct);
            if (curso == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Curso no encontrado");

            if (curso.Estado == EstadoCurso.Inactivo)
                return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "No se puede asignar profesores a un curso inactivo");
            if (curso.Estado == EstadoCurso.Archivado)
                return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "No se puede asignar profesores a un curso archivado");

            var profesorIds = model.ProfesorIds.Distinct().ToList();

            // Validar que existan profesores
            var profesores = await _db.Profesores
                .Where(p => profesorIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync(ct);

            var notFound = profesorIds.Except(profesores).ToList();
            if (notFound.Count > 0)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, new { profesorIdsNoEncontrados = notFound }, "Hay profesores que no existen");

            // Validar rol Profesor
            foreach (var pid in profesores)
            {
                var user = await _userManager.FindByIdAsync(pid.ToString());
                if (user == null || !user.Activo)
                    return ResponseApiService.Response(StatusCodes.Status409Conflict, message: $"El profesor {pid} está inactivo o no existe como usuario");

                if (!await _userManager.IsInRoleAsync(user, "Profesor"))
                    return ResponseApiService.Response(StatusCodes.Status409Conflict, message: $"El usuario {pid} no tiene rol Profesor");
            }

            // Ya existentes
            var existentes = await _db.CursoProfesores
                .Where(cp => cp.CursoId == cursoId && profesorIds.Contains(cp.ProfesorId))
                .Select(cp => cp.ProfesorId)
                .ToListAsync(ct);

            var nuevos = profesorIds.Except(existentes).ToList();

            await using var tx = await _db.BeginTransactionAsync(ct);

            foreach (var pid in nuevos)
            {
                _db.CursoProfesores.Add(new CursoProfesorEntity
                {
                    CursoId = cursoId,
                    ProfesorId = pid
                });
            }

            await _db.SaveAsync(ct);
            await tx.CommitAsync(ct);

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                cursoId,
                agregados = nuevos,
                yaExistian = existentes
            }, "Profesores asignados");
        }
    }
}
