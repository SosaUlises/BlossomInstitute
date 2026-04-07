using BlossomInstitute.Domain.Entidades.Tarea;
using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Tarea.Queries.GetTareasByCurso
{
    public interface IGetTareasByCursoQuery
    {
        Task<BaseResponseModel> Execute(
             int cursoId,
             int userId,
             int pageNumber,
             int pageSize,
             string? search,
             EstadoTarea? estado,
             CancellationToken ct = default);
    }
}
