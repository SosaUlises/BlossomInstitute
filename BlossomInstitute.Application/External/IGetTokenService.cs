using BlossomInstitute.Domain.Entidades.Usuario;

namespace BlossomInstitute.Application.External
{
    public interface IGetTokenJWTService
    {
        string Execute(string userId, IEnumerable<string> roles, UsuarioEntity usuario);
    }
}
