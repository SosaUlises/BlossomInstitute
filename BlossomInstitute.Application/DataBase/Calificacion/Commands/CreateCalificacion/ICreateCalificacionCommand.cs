using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Calificacion.Commands.CreateCalificacion
{
    public interface ICreateCalificacionCommand
    {
        Task<BaseResponseModel> Execute(
              int cursoId,
              int alumnoId,
              int profesorUserId,
              CreateCalificacionModel? model,
              CancellationToken ct);
    }
}
