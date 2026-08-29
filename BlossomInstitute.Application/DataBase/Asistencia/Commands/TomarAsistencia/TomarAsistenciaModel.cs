namespace BlossomInstitute.Application.DataBase.Asistencia.Commands.TomarAsistencia
{
    public class TomarAsistenciaModel
    {
        public List<TomarAsistenciaItemModel> Asistencias { get; set; } = new();
        public string? DescripcionClase { get; set; }
    }
}
