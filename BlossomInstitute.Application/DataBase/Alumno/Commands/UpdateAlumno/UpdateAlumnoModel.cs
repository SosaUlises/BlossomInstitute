namespace BlossomInstitute.Application.DataBase.Alumno.Commands.UpdateAlumno
{
    public class UpdateAlumnoModel
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public long Dni { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public string? Password { get; set; }
    }
}
