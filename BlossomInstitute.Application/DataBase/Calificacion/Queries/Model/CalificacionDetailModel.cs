using BlossomInstitute.Domain.Entidades.Calificacion;

namespace BlossomInstitute.Application.DataBase.Calificacion.Queries.Model
{
    public class CalificacionDetailModel
    {
        public int Id { get; set; }
        public int CursoId { get; set; }
        public int AlumnoId { get; set; }

        public TipoCalificacion Tipo { get; set; }
        public string Titulo { get; set; } = "";
        public string? Descripcion { get; set; }

        public decimal Nota { get; set; }
        public DateOnly Fecha { get; set; }

        public int? TareaId { get; set; }
        public int? EntregaId { get; set; }

        public bool TieneDetalleSkills { get; set; }
        public List<CalificacionDetalleItemModel> Detalles { get; set; } = new();
    }

    public class CalificacionDetalleItemModel
    {
        public SkillEvaluada Skill { get; set; }
        public decimal PuntajeObtenido { get; set; }
        public decimal PuntajeMaximo { get; set; }
    }
}
