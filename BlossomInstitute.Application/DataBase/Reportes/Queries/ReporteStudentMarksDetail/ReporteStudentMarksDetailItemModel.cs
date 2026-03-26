using BlossomInstitute.Domain.Entidades.Calificacion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteStudentMarksDetail
{
    public class ReporteStudentMarksDetailItemModel
    {
        public int CalificacionId { get; set; }
        public TipoCalificacion Tipo { get; set; }

        public string Titulo { get; set; } = default!;
        public string? Descripcion { get; set; }

        public decimal Nota { get; set; }
        public DateOnly Fecha { get; set; } 
        public int? TareaId { get; set; }
        public int? EntregaId { get; set; }

        public bool TieneDetalleSkills { get; set; }

        public List<ReporteStudentMarksDetailSkillModel> Skills { get; set; } = new();
    }

}
