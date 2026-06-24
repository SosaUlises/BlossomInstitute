using BlossomInstitute.Domain.Entidades.Clase;

namespace BlossomInstitute.Domain.Entidades.Curso
{
    public class CursoEntity
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = default!;
        public int Anio { get; set; }
        public string? Descripcion { get; set; }
        public string? ThemeIcon { get; set; }
        public EstadoCurso Estado { get; set; } = EstadoCurso.Activo;

        public List<CursoHorarioEntity> Horarios { get; set; } = new();
        public List<CursoProfesorEntity> Profesores { get; set; } = new();
        public List<MatriculaEntity> Matriculas { get; set; } = new();
        public ICollection<ClaseEntity> Clases { get; set; }
    }
}
