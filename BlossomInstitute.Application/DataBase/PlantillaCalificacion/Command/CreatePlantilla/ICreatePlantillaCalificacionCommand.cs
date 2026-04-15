using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Command.CreatePlantilla
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
