namespace BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteAsistenciaByClase
{
    public class AsistenciaReporteModel
    {
        public int CursoId { get; set; }
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public int TotalAlumnos { get; set; }
        public int TotalClasesConAsistencia { get; set; }
        public List<AsistenciaReporteItemModel> Items { get; set; } = new();
    }
}
