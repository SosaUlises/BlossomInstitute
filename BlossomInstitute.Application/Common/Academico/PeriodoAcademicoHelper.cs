namespace BlossomInstitute.Application.Common.Academico
{
    public static class PeriodoAcademicoHelper
    {
        public static ContextoPeriodoAcademico ObtenerContexto(DateOnly fecha)
        {
            var trimestreActual = ObtenerActual(fecha);
            var trimestreAnterior = ObtenerAnterior(trimestreActual);
            var hasta = AjustarAlPeriodo(fecha, trimestreActual);

            return new ContextoPeriodoAcademico
            {
                TrimestreActual = trimestreActual,
                TrimestreAnterior = trimestreAnterior,
                Desde = trimestreActual.Desde,
                Hasta = hasta,
                Etiqueta = trimestreActual.Etiqueta,
                Anio = trimestreActual.Anio,
                NumeroTrimestre = trimestreActual.Trimestre
            };
        }

        public static PeriodoAcademicoTrimestre ObtenerActual(DateOnly fecha)
        {
            return fecha.Month switch
            {
                >= 3 and <= 5 => Crear(fecha.Year, 1),
                >= 6 and <= 8 => Crear(fecha.Year, 2),
                >= 9 and <= 11 => Crear(fecha.Year, 3),
                12 => Crear(fecha.Year, 3),
                _ => Crear(fecha.Year - 1, 3)
            };
        }

        public static PeriodoAcademicoTrimestre ObtenerAnterior(PeriodoAcademicoTrimestre periodo)
        {
            return periodo.Trimestre switch
            {
                1 => Crear(periodo.Anio - 1, 3),
                2 => Crear(periodo.Anio, 1),
                _ => Crear(periodo.Anio, 2)
            };
        }

        public static PeriodoAcademicoTrimestre ObtenerTrimestre(int anio, int trimestre)
        {
            return Crear(anio, trimestre);
        }

        public static DateOnly AjustarAlPeriodo(DateOnly fecha, PeriodoAcademicoTrimestre periodo)
        {
            if (fecha < periodo.Desde) return periodo.Hasta;
            if (fecha > periodo.Hasta) return periodo.Hasta;
            return fecha;
        }

        private static PeriodoAcademicoTrimestre Crear(int anio, int trimestre)
        {
            var mesInicio = trimestre switch
            {
                1 => 3,
                2 => 6,
                3 => 9,
                _ => throw new ArgumentOutOfRangeException(nameof(trimestre), "El trimestre academico debe ser 1, 2 o 3.")
            };

            var desde = new DateOnly(anio, mesInicio, 1);
            var hasta = desde.AddMonths(3).AddDays(-1);

            return new PeriodoAcademicoTrimestre
            {
                Anio = anio,
                Trimestre = trimestre,
                Desde = desde,
                Hasta = hasta,
                Etiqueta = trimestre switch
                {
                    1 => "Primer trimestre",
                    2 => "Segundo trimestre",
                    _ => "Tercer trimestre"
                },
                EtiquetaRangoMeses = trimestre switch
                {
                    1 => "Marzo a mayo",
                    2 => "Junio a agosto",
                    _ => "Septiembre a noviembre"
                }
            };
        }
    }

    public sealed class PeriodoAcademicoTrimestre
    {
        public int Anio { get; init; }
        public int Trimestre { get; init; }
        public DateOnly Desde { get; init; }
        public DateOnly Hasta { get; init; }
        public string Etiqueta { get; init; } = default!;
        public string EtiquetaRangoMeses { get; init; } = default!;
    }

    public sealed class ContextoPeriodoAcademico
    {
        public PeriodoAcademicoTrimestre TrimestreActual { get; init; } = default!;
        public PeriodoAcademicoTrimestre TrimestreAnterior { get; init; } = default!;
        public DateOnly Desde { get; init; }
        public DateOnly Hasta { get; init; }
        public string Etiqueta { get; init; } = default!;
        public int Anio { get; init; }
        public int NumeroTrimestre { get; init; }
    }
}
