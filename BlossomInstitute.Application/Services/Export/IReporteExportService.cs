using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteAttendanceByCursoAndTerm;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteHomeworkByCursoAndTerm;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteMarksByCursoAndTerm;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteStudentMarksDetail;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteStudentSummaryByCursoAndTerm;

namespace BlossomInstitute.Application.Services.Export
{
    public interface IReporteExportService
    {
        byte[] ExportarCalificacionesPorCursoYTrimestreAExcel(
            ReporteMarksByCursoAndTermResumenModel resumen,
            List<ReporteMarksByCursoAndTermItemModel> items);

        byte[] ExportarCalificacionesPorCursoYTrimestreAPdf(
            ReporteMarksByCursoAndTermResumenModel resumen,
            List<ReporteMarksByCursoAndTermItemModel> items);



        byte[] ExportarAsistenciaPorCursoYTrimestreAExcel(
        ReporteAttendanceByCursoAndTermResumenModel resumen,
        List<ReporteAttendanceByCursoAndTermItemModel> items);

        byte[] ExportarAsistenciaPorCursoYTrimestreAPdf(
            ReporteAttendanceByCursoAndTermResumenModel resumen,
            List<ReporteAttendanceByCursoAndTermItemModel> items);

        byte[] ExportarDetalleEvaluacionesAlumnoPorCursoYTrimestreAPdf(
             ReporteStudentMarksDetailResponseModel data);

        byte[] ExportarTareasPorCursoYTrimestreAExcel(
        ReporteHomeworkByCursoAndTermResumenModel resumen,
        List<ReporteHomeworkByCursoAndTermItemModel> items);

        byte[] ExportarTareasPorCursoYTrimestreAPdf(
            ReporteHomeworkByCursoAndTermResumenModel resumen,
            List<ReporteHomeworkByCursoAndTermItemModel> items);


        byte[] ExportarResumenAlumnoPorCursoYTrimestreAPdf(
            ReporteStudentSummaryByCursoAndTermResponseModel data);
    }
}
