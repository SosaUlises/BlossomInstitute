using BlossomInstitute.Domain.Entidades.Alumno;
using BlossomInstitute.Domain.Entidades.Profesor;
using Microsoft.AspNetCore.Identity;

namespace BlossomInstitute.Domain.Entidades.Usuario
{
    public class UsuarioEntity : IdentityUser<int>
    {
        public bool Activo { get; set; } = true;
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public long Dni { get; set; }
        public string? AvatarUrl { get; set; }
        public string? AvatarPublicId { get; set; }

        public ProfesorEntity? Profesor { get; set; }
        public AlumnoEntity? Alumno { get; set; }
    }
}
