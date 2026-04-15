using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Command.Archive
{
    public interface IArchivePlantillaCalificacionCommand
    {
        Task<BaseResponseModel> Execute(
            int cursoId,
            int plantillaId,
            int profesorUserId,
            CancellationToken ct);
    }
}
