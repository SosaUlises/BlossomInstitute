using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Tarea.Queries.GetTareasAlumno
{
    public interface IGetTareaAlumnoByIdQuery
    {
        Task<BaseResponseModel> Execute(int cursoId, int tareaId, int alumnoUserId, CancellationToken ct = default);
    }
}
