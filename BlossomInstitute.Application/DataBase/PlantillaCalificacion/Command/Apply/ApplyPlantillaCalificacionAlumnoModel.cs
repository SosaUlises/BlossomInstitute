using BlossomInstitute.Application.DataBase.PlantillaCalificacion.Command.Apply;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Command.Apply
{
    public class ApplyPlantillaCalificacionAlumnoModel
    {
        public int AlumnoId { get; set; }
        public List<ApplyPlantillaCalificacionAlumnoDetalleModel> Detalles { get; set; } = new();
    }
}
