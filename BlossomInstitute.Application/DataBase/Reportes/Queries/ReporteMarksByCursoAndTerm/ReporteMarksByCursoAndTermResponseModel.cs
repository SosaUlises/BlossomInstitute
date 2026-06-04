using BlossomInstitute.Application.DataBase.Reportes.Shared;

namespace BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteMarksByCursoAndTerm
{
    public class ReporteMarksByCursoAndTermResponseModel
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public ReportAcademicPeriodModel Period { get; set; } = new();

        public ReporteMarksByCursoAndTermResumenModel Resumen { get; set; } = new();

        public List<ReporteMarksByCursoAndTermItemModel> Items { get; set; } = new();
    }
}
