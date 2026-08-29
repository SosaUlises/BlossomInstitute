using BlossomInstitute.Domain.Entidades.Usuario;

namespace BlossomInstitute.Domain.Entidades.Profesor
{
    public class ProfesorEntity
    {
        public int Id { get; set; }
        public UsuarioEntity Usuario { get; set; } = default!;
    }
}
