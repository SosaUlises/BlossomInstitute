using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Alumno.Queries.GetAll
{
    public interface IGetAllAlumnosQuery
    {
        Task<BaseResponseModel> Execute(
            int pageNumber,
            int pageSize,
            string? search,
            CancellationToken ct = default);
    }
}
