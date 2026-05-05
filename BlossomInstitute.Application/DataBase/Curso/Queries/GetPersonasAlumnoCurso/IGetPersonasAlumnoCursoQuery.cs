using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Curso.Queries.GetPersonasAlumnoCurso
{
    public interface IGetPersonasAlumnoCursoQuery
    {
        Task<BaseResponseModel> Execute(int cursoId, int alumnoUserId, CancellationToken ct = default);
    }
}
