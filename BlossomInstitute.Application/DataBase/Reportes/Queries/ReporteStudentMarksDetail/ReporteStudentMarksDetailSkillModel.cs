using BlossomInstitute.Domain.Entidades.Calificacion;

namespace BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteStudentMarksDetail
{
    public class ReporteStudentMarksDetailSkillModel
    {
        public SkillEvaluada Skill { get; set; }
        public decimal PuntajeObtenido { get; set; }
        public decimal PuntajeMaximo { get; set; }
        public decimal? Porcentaje { get; set; }
    }
}
