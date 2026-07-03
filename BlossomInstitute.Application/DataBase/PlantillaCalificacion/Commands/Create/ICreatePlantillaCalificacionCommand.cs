using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Commands.Create
{
    public interface ICreatePlantillaCalificacionCommand
    {
        Task<BaseResponseModel> Execute(
            int cursoId,
            int profesorUserId,
            CreatePlantillaCalificacionModel model,
            CancellationToken ct);
    }
}
