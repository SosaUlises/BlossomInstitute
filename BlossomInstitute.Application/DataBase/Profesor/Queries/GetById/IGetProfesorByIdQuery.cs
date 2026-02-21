using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Profesor.Queries.GetById
{
    public interface IGetProfesorByIdQuery
    {
        Task<BaseResponseModel> Execute(int userId);
    }
}
