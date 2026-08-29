using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Asistencia.Queries.GetAsistenciasByClase
{
    public interface IGetAsistenciasByClaseQuery
    {
        Task<BaseResponseModel> Execute(int cursoId, DateOnly fecha, CancellationToken ct = default);
    }
}
