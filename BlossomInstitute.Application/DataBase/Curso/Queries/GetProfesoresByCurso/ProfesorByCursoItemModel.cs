namespace BlossomInstitute.Application.DataBase.Curso.Queries.GetProfesoresByCurso
{
    public class ProfesorByCursoItemModel
    {
        public int ProfesorId { get; set; }
        public string Nombre { get; set; } = default!;
        public string Apellido { get; set; } = default!;
        public long Dni { get; set; }
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
