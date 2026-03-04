using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Entregas.Queries.GetEntregasByTarea
{
    public interface IGetEntregasByTareaQuery
    {
        Task<BaseResponseModel> Execute(
            int cursoId,
            int tareaId,
            int profesorUserId,
            int pageNumber,
            int pageSize,
            string? search,
            CancellationToken ct);
    }
}
