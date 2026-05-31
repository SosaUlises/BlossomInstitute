using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Profesor.Queries.GetAcademicSummary
{
    public interface IGetProfesorAcademicSummaryQuery
    {
        Task<BaseResponseModel> Execute(int teacherId, CancellationToken ct);
    }
}
