using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Alumno.Queries.GetAsignableByCurso
{
    public interface IGetAsignableAlumnosByCursoQuery
    {
        Task<BaseResponseModel> Execute(int cursoId, int pageNumber, int pageSize, string? search);
    }
}
