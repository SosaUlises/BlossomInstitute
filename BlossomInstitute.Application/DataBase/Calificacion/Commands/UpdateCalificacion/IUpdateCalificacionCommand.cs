using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Calificacion.Commands.UpdateCalificacion
{
    public interface IUpdateCalificacionCommand
    {
        Task<BaseResponseModel> Execute(
            int cursoId,
            int alumnoId,
            int calificacionId,
            int profesorUserId,
            UpdateCalificacionModel model,
            CancellationToken ct);
    }
}
