using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Curso.Commands.ActivarCurso
{
    public class ActivateCursoCommand : IActivateCursoCommand
    {
        private readonly IDataBaseService _db;

        public ActivateCursoCommand(IDataBaseService db)
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
                return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "No se puede activar un curso archivado");

            if (curso.Estado == EstadoCurso.Activo)
                return ResponseApiService.Response(StatusCodes.Status200OK, message: "El curso ya se encuentra activo");

            curso.Estado = EstadoCurso.Activo;

            await _db.SaveAsync();

            return ResponseApiService.Response(StatusCodes.Status200OK, message: "Curso activado correctamente");
        }
    }
}
