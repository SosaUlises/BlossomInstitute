namespace BlossomInstitute.Application.DataBase.Alumno.Commands.CreateAlumno
{
    public class CreateAlumnoModel
    {
        public string Nombre { get; set; } = default!;
        public string Apellido { get; set; } = default!;
        public long Dni { get; set; }
        public string Telefono { get; set; } = default!;
        public string Email { get; set; } = default!; 
        public string Password { get; set; } = default!;
    }
}
