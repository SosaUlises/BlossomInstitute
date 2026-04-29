using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Curso.Commands.RemoveProfesores
{
    public class RemoveProfesorFromCursoCommand : IRemoveProfesorFromCursoCommand
    {
        private readonly IDataBaseService _db;

        public RemoveProfesorFromCursoCommand(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int cursoId, int profesorId, CancellationToken ct = default)
        {
            if (cursoId <= 0 || profesorId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Ids inválidos");

            var rel = await _db.CursoProfesores
                .FirstOrDefaultAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorId, ct);

            if (rel == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "La asignación no existe");

            await using var tx = await _db.BeginTransactionAsync(ct);
            _db.CursoProfesores.Remove(rel);
            await _db.SaveAsync(ct);
            await tx.CommitAsync(ct);

            return ResponseApiService.Response(StatusCodes.Status200OK, new { cursoId, profesorId }, "Profesor removido del curso");
        }
    }
}
