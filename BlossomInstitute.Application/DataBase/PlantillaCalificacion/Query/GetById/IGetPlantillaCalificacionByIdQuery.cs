using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Query.GetById
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
