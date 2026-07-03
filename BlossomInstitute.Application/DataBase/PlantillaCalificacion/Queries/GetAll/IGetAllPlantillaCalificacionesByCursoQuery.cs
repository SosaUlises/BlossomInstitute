using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Queries.GetAll
{
    public interface IGetAllPlantillaCalificacionesByCursoQuery
    {
        Task<BaseResponseModel> Execute(
            int cursoId,
            int profesorUserId,
            int pageNumber,
            int pageSize,
            string? search,
            CancellationToken ct);
    }
}
