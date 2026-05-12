using BlossomInstitute.Domain.Entidades.Calificacion;

namespace BlossomInstitute.Application.DataBase.Calificacion.Queries.Model
{
    public class CalificacionListItemModel
    {
        public int Id { get; set; }
        public int CursoId { get; set; }
        public int AlumnoId { get; set; }

        public string CursoNombre { get; set; } = "";
        public string AlumnoNombre { get; set; } = "";
        public string AlumnoApellido { get; set; } = "";
        public long AlumnoDni { get; set; }

        public TipoCalificacion Tipo { get; set; }
        public string Titulo { get; set; } = "";
        public string? Descripcion { get; set; }

        public decimal Nota { get; set; }
        public DateOnly Fecha { get; set; }

        public int? TareaId { get; set; }
        public int? EntregaId { get; set; }

        public bool TieneDetalleSkills { get; set; }
        public List<CalificacionListDetalleItemModel> Detalles { get; set; } = new();
    }

    public class CalificacionListDetalleItemModel
    {
        public SkillEvaluada Skill { get; set; }
        public decimal PuntajeObtenido { get; set; }
        public decimal PuntajeMaximo { get; set; }
        public decimal? Porcentaje { get; set; }
    }
}
