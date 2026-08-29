using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Asistencia.Queries.GetMisAsistencias
{
    public interface IGetMisAsistenciasQuery
    {
        Task<BaseResponseModel> Execute(
            int userId,
            int? cursoId,
            DateOnly? fechaDesde,
            DateOnly? fechaHasta,
            int pageNumber,
            int pageSize,
            CancellationToken ct = default);
    }
}
