namespace BlossomInstitute.Application.DataBase.Curso.Queries.GetAlumnosByCurso
{
    public class AlumnoByCursoItemModel
    {
        public int AlumnoId { get; set; }
        public string Nombre { get; set; } = default!;
        public string Apellido { get; set; } = default!;
        public long Dni { get; set; }
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
