using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteEntregaByTarea
{
    public interface IGetReporteEntregasByTareaQuery
    {
        Task<BaseResponseModel> Execute(
            int cursoId,
            int tareaId,
            int profesorUserId,
            bool isAdmin,
            int pageNumber,
            int pageSize,
            string? search,
            EstadoEntregaReporte? estado,
            bool? pendienteCorreccion,
            CancellationToken ct);
    }
}
