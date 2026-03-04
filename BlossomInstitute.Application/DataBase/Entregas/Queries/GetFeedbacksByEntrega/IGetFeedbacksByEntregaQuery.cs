using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Entregas.Queries.GetFeedbacksByEntrega
{
    public interface IGetFeedbacksByEntregaQuery
    {
        Task<BaseResponseModel> Execute(int cursoId, int tareaId, int alumnoId, int profesorUserId, CancellationToken ct);
    }
}
