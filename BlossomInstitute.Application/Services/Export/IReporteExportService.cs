using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteAttendanceByCursoAndTerm;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteHomeworkByCursoAndTerm;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteMarksByCursoAndTerm;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteStudentMarksDetail;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteStudentSummaryByCursoAndTerm;

namespace BlossomInstitute.Application.Services.Export
{
    public interface IReporteExportService
    {
        byte[] ExportMarksByCourseTermToExcel(
            ReporteMarksByCursoAndTermResumenModel resumen,
            List<ReporteMarksByCursoAndTermItemModel> items);

        byte[] ExportMarksByCourseTermToPdf(
            ReporteMarksByCursoAndTermResumenModel resumen,
            List<ReporteMarksByCursoAndTermItemModel> items);



        byte[] ExportAttendanceByCourseTermToExcel(
        ReporteAttendanceByCursoAndTermResumenModel resumen,
        List<ReporteAttendanceByCursoAndTermItemModel> items);

        byte[] ExportAttendanceByCourseTermToPdf(
            ReporteAttendanceByCursoAndTermResumenModel resumen,
            List<ReporteAttendanceByCursoAndTermItemModel> items);

        byte[] ExportStudentAssessmentsDetailByCourseTermToPdf(
             ReporteStudentMarksDetailResponseModel data);

        byte[] ExportHomeworkByCourseTermToExcel(
        ReporteHomeworkByCursoAndTermResumenModel resumen,
        List<ReporteHomeworkByCursoAndTermItemModel> items);

        byte[] ExportHomeworkByCourseTermToPdf(
            ReporteHomeworkByCursoAndTermResumenModel resumen,
            List<ReporteHomeworkByCursoAndTermItemModel> items);


        byte[] ExportStudentSummaryByCourseTermToPdf(
            ReporteStudentSummaryByCursoAndTermResponseModel data);
    }
}
