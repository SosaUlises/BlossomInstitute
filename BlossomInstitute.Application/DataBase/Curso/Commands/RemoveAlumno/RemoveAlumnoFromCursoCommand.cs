using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Curso.Commands.RemoveAlumno
{
    public class RemoveAlumnoFromCursoCommand : IRemoveAlumnoFromCursoCommand
    {
        private readonly IDataBaseService _db;

        public RemoveAlumnoFromCursoCommand(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int cursoId, int alumnoId, CancellationToken ct = default)
        {
            if (cursoId <= 0 || alumnoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Ids inválidos");

            var rel = await _db.Matriculas
                .FirstOrDefaultAsync(x => x.CursoId == cursoId && x.AlumnoId == alumnoId, ct);

            if (rel == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "La matrícula no existe");

            await using var tx = await _db.BeginTransactionAsync(ct);
            _db.Matriculas.Remove(rel);
            await _db.SaveAsync(ct);
            await tx.CommitAsync(ct);

            return ResponseApiService.Response(StatusCodes.Status200OK, new { cursoId, alumnoId }, "Alumno desmatriculado");
        }
    }
}
