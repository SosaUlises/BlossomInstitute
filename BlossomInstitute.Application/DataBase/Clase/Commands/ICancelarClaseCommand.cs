using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Clase.Commands
{
    public interface ICancelarClaseCommand
    {
        Task<BaseResponseModel> Execute(int cursoId, DateOnly fecha, CancellationToken ct = default);
    }
}
