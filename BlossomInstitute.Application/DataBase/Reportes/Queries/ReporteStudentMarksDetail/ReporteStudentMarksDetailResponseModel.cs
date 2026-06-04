using BlossomInstitute.Application.DataBase.Reportes.Shared;

namespace BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteStudentMarksDetail
{
    public class ReporteStudentMarksDetailResponseModel
    {
        public int CursoId { get; set; }
        public string CursoNombre { get; set; } = default!;

        public int AlumnoId { get; set; }
        public string AlumnoNombre { get; set; } = default!;
        public string AlumnoApellido { get; set; } = default!;
        public long AlumnoDni { get; set; }
        public string? AlumnoEmail { get; set; }

        public int Year { get; set; }
        public int Term { get; set; }
        public DateOnly From { get; set; }
        public DateOnly To { get; set; }
        public ReportAcademicPeriodModel Period { get; set; } = new();

        public int Total { get; set; }

        public List<ReporteStudentMarksDetailItemModel> Items { get; set; } = new();
    }
}
