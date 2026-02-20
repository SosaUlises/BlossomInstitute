using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.Profesor.Command.UpdateProfesor
{
    public class UpdateProfesorModel
    {
        public string Nombre { get; set; } = default!;
        public string Apellido { get; set; } = default!;
        public long Dni { get; set; }
        public string Telefono { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? Password { get; set; }
    }
}
