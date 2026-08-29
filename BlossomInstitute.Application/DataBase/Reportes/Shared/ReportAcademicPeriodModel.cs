using BlossomInstitute.Application.Common.Academico;

namespace BlossomInstitute.Application.DataBase.Reportes.Shared
{
    public class ReportAcademicPeriodModel
    {
        public int Year { get; set; }
        public int Quarter { get; set; }
        public string Label { get; set; } = default!;
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }

        public static ReportAcademicPeriodModel FromQuarter(PeriodoAcademicoTrimestre period)
        {
            return new ReportAcademicPeriodModel
            {
                Year = period.Anio,
                Quarter = period.Trimestre,
                Label = period.Etiqueta,
                From = period.Desde,
                To = period.Hasta
            };
        }
    }
}
