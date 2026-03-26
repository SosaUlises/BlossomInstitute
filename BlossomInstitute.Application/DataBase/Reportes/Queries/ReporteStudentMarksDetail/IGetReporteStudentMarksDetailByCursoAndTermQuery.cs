using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteStudentMarksDetail
{
    public interface IGetReporteStudentMarksDetailByCursoAndTermQuery
    {
        Task<BaseResponseModel> Execute(
            int cursoId,
            int alumnoId,
            int year,
            int term,
            int userId,
            bool isAdmin,
            int? tipo,
            CancellationToken ct);
    }
}
