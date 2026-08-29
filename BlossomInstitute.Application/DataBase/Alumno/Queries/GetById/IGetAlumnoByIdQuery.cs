using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Alumno.Queries.GetById
{
    public interface IGetAlumnoByIdQuery
    {
        Task<BaseResponseModel> Execute(int userId, CancellationToken ct = default);
    }
}
