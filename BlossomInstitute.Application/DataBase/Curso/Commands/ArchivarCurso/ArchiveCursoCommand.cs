using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Curso.Commands.ArchivarCurso
{
    public class ArchiveCursoCommand : IArchiveCursoCommand
    {
        private readonly IDataBaseService _db;

        public ArchiveCursoCommand(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int cursoId)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Id inválido");

            var curso = await _db.Cursos.FirstOrDefaultAsync(c => c.Id == cursoId);

            if (curso == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Curso no encontrado");

            if (curso.Estado == EstadoCurso.Archivado)
                return ResponseApiService.Response(StatusCodes.Status200OK, message: "El curso ya se encuentra archivado");

            curso.Estado = EstadoCurso.Archivado;

            await _db.SaveAsync();

            return ResponseApiService.Response(StatusCodes.Status200OK, message: "Curso archivado correctamente");
        }
    }
}
