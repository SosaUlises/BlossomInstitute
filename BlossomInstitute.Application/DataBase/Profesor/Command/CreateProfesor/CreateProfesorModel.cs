namespace BlossomInstitute.Application.DataBase.Profesor.Command.CreateProfesor
{
    public class CreateProfesorModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public long Dni { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
