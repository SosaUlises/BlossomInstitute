using BlossomInstitute.Domain.Entidades.Calificacion;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Queries.Models
{
    public class PlantillaCalificacionDetalleModel
    {
        public int Id { get; set; }
        public SkillEvaluada Skill { get; set; }
        public decimal PuntajeMaximo { get; set; }
    }
}

