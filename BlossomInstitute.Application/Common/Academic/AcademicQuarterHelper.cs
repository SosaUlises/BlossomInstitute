namespace BlossomInstitute.Application.Common.Academic
{
    public static class AcademicQuarterHelper
    {
        public static AcademicPeriodContext GetContext(DateOnly date)
        {
            var currentQuarter = GetCurrent(date);
            var previousQuarter = GetPrevious(currentQuarter);
            var dataTo = ClampToPeriod(date, currentQuarter);

            return new AcademicPeriodContext
            {
                CurrentQuarter = currentQuarter,
                PreviousQuarter = previousQuarter,
                From = currentQuarter.From,
                To = dataTo,
                Label = currentQuarter.Label,
                Year = currentQuarter.Year,
                QuarterNumber = currentQuarter.Quarter
            };
        }

        public static AcademicQuarterPeriod GetCurrent(DateOnly date)
        {
            return date.Month switch
            {
                >= 3 and <= 5 => Create(date.Year, 1),
                >= 6 and <= 8 => Create(date.Year, 2),
                >= 9 and <= 11 => Create(date.Year, 3),
                12 => Create(date.Year, 3),
                _ => Create(date.Year - 1, 3)
            };
        }

        public static AcademicQuarterPeriod GetPrevious(AcademicQuarterPeriod period)
        {
            return period.Quarter switch
            {
                1 => Create(period.Year - 1, 3),
                2 => Create(period.Year, 1),
                _ => Create(period.Year, 2)
            };
        }

        public static AcademicQuarterPeriod GetQuarter(int year, int quarter)
        {
            return Create(year, quarter);
        }

        public static DateOnly ClampToPeriod(DateOnly date, AcademicQuarterPeriod period)
        {
            if (date < period.From) return period.To;
            if (date > period.To) return period.To;
            return date;
        }

        private static AcademicQuarterPeriod Create(int year, int quarter)
        {
            var startMonth = quarter switch
            {
                1 => 3,
                2 => 6,
                3 => 9,
                _ => throw new ArgumentOutOfRangeException(nameof(quarter), "El trimestre académico debe ser 1, 2 o 3.")
            };

            var from = new DateOnly(year, startMonth, 1);
            var to = from.AddMonths(3).AddDays(-1);

            return new AcademicQuarterPeriod
            {
                Year = year,
                Quarter = quarter,
                From = from,
                To = to,
                Label = $"{quarter}º trimestre",
                MonthRangeLabel = quarter switch
                {
                    1 => "Marzo a mayo",
                    2 => "Junio a agosto",
                    _ => "Septiembre a noviembre"
                }
            };
        }
    }

    public sealed class AcademicQuarterPeriod
    {
        public int Year { get; init; }
        public int Quarter { get; init; }
        public DateOnly From { get; init; }
        public DateOnly To { get; init; }
        public string Label { get; init; } = default!;
        public string MonthRangeLabel { get; init; } = default!;
    }

    public sealed class AcademicPeriodContext
    {
        public AcademicQuarterPeriod CurrentQuarter { get; init; } = default!;
        public AcademicQuarterPeriod PreviousQuarter { get; init; } = default!;
        public DateOnly From { get; init; }
        public DateOnly To { get; init; }
        public string Label { get; init; } = default!;
        public int Year { get; init; }
        public int QuarterNumber { get; init; }
    }
}
