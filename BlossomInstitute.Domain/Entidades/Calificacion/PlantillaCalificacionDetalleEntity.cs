namespace BlossomInstitute.Domain.Entidades.Calificacion
{
    public class PlantillaCalificacionDetalleEntity
    {
        public int Id { get; set; }

        public int PlantillaCalificacionId { get; set; }
        public PlantillaCalificacionEntity PlantillaCalificacion { get; set; } = default!;

        public SkillEvaluada Skill { get; set; }
        public decimal PuntajeMaximo { get; set; }
    }
}
