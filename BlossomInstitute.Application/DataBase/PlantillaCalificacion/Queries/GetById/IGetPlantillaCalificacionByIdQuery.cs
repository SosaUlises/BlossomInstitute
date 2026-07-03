using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Queries.GetById
{
    public interface IGetPlantillaCalificacionByIdQuery
    {
        Task<BaseResponseModel> Execute(
            int cursoId,
            int plantillaId,
            int profesorUserId,
            CancellationToken ct);
    }
}
