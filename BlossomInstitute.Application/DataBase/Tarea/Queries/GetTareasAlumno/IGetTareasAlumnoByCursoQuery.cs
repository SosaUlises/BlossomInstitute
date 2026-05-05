using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Tarea.Queries.GetTareasAlumno
{
    public interface IGetTareasAlumnoByCursoQuery
    {
        Task<BaseResponseModel> Execute(int cursoId, int alumnoUserId, int pageNumber, int pageSize, CancellationToken ct = default);
    }
}
