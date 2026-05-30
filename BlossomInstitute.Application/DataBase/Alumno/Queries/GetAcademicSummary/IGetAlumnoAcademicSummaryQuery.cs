using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Alumno.Queries.GetAcademicSummary
{
    public interface IGetAlumnoAcademicSummaryQuery
    {
        Task<BaseResponseModel> Execute(int studentId, CancellationToken ct);
    }
}
