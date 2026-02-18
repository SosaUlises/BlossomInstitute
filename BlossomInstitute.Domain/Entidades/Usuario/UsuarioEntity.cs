using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Domain.Entidades.Usuario
{
    public class UsuarioEntity : IdentityUser<int>
    {
        public bool Activo { get; set; } = true;
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
    }
}
