using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Asistencia.Commands.TomarAsistencia
{
    public interface ITomarAsistenciaCommand
    {
        Task<BaseResponseModel> Execute(int cursoId, DateOnly fecha, TomarAsistenciaModel? model, CancellationToken ct = default);
    }
}
