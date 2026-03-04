using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Entregas.Queries.Alumno.GetMisEntregasByCurso
{
    public interface IGetMisEntregasByCursoQuery
    {
        Task<BaseResponseModel> Execute(int cursoId, int alumnoUserId, int pageNumber, int pageSize, CancellationToken ct);
    }
}
