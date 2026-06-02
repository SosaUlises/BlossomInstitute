using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Curso.Queries.GetAcademicProfile
{
    public interface IGetCourseAcademicProfileQuery
    {
        Task<BaseResponseModel> Execute(int courseId, CancellationToken ct);
    }
}
