namespace BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteAsistenciaByClase
{
    public class AsistenciaReporteItemModel
    {
        public int AlumnoId { get; set; }
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public int Presentes { get; set; }
        public int Ausentes { get; set; }
        public int TotalRegistradas { get; set; }
        public decimal PorcentajePresencia { get; set; }
    }
}
