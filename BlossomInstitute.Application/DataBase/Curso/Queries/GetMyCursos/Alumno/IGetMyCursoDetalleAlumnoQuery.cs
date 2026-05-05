using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Curso.Queries.GetMyCursos.Alumno
{
    public interface IGetMyCursoDetalleAlumnoQuery
    {
        Task<BaseResponseModel> Execute(int userId, int cursoId, CancellationToken ct = default);
    }
}
