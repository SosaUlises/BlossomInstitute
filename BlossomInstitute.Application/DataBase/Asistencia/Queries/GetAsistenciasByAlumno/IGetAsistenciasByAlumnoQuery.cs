using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Asistencia.Queries.GetAsistenciasByAlumno
{
    public interface IGetAsistenciasByAlumnoQuery
    {
        Task<BaseResponseModel> Execute(int alumnoId, int cursoId, DateOnly? fechaDesde, DateOnly? fechaHasta, CancellationToken ct = default);
    }
}
