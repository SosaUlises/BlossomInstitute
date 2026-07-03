using BlossomInstitute.Application.DataBase.PlantillaCalificacion.Commands.Apply;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Commands.Apply
{
    public class ApplyPlantillaCalificacionModel
    {
        public DateOnly Fecha { get; set; }
        public List<ApplyPlantillaCalificacionAlumnoModel> Alumnos { get; set; } = new();
    }
}
