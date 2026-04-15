using BlossomInstitute.Domain.Entidades.Calificacion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Command.Apply
{
    public class ApplyPlantillaCalificacionAlumnoDetalleModel
    {
        public SkillEvaluada Skill { get; set; }
        public decimal PuntajeObtenido { get; set; }
    }
}
