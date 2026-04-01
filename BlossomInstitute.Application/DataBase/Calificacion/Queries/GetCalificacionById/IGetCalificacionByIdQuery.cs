using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Calificacion.Queries.GetCalificacionById
{
    public interface IGetCalificacionByIdQuery
    {
        Task<BaseResponseModel> Execute(
            int cursoId,
            int alumnoId,
            int calificacionId,
            int userId,
            bool isAdmin,
            bool isProfesor,
            CancellationToken ct);
    }
}
