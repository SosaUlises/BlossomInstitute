using BlossomInstitute.Domain.Entidades.Calificacion;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Query.Models
{
    public class PlantillaCalificacionDetalleModel
    {
        public int Id { get; set; }
        public SkillEvaluada Skill { get; set; }
        public decimal PuntajeMaximo { get; set; }
    }
}

