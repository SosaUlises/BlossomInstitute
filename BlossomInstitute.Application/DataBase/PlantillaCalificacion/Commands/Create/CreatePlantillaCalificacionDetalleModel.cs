using BlossomInstitute.Domain.Entidades.Calificacion;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Commands.Create
{
    public class CreatePlantillaCalificacionDetalleModel
    {
        public SkillEvaluada Skill { get; set; }
        public decimal PuntajeMaximo { get; set; }

    }
}
