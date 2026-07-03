using BlossomInstitute.Domain.Entidades.Calificacion;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Commands.Update
{
    public class UpdatePlantillaCalificacionDetalleModel
    {
        public SkillEvaluada Skill { get; set; }
        public decimal PuntajeMaximo { get; set; }
    }
}
