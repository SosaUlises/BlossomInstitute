using BlossomInstitute.Application.Common.Academic;

namespace BlossomInstitute.Application.DataBase.Reportes.Shared
{
    public class ReportAcademicPeriodModel
    {
        public int Year { get; set; }
        public int Quarter { get; set; }
        public string Label { get; set; } = default!;
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }

        public static ReportAcademicPeriodModel FromQuarter(AcademicQuarterPeriod period)
        {
            return new ReportAcademicPeriodModel
            {
                Year = period.Year,
                Quarter = period.Quarter,
                Label = period.Label,
                From = period.From,
                To = period.To
            };
        }
    }
}
