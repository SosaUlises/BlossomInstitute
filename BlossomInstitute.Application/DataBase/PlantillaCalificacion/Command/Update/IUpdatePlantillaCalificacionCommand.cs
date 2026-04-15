using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Command.Update
{
    public interface IUpdatePlantillaCalificacionCommand
    {
        Task<BaseResponseModel> Execute(
            int cursoId,
            int plantillaId,
            int profesorUserId,
            UpdatePlantillaCalificacionModel model,
            CancellationToken ct);
    }
}
