using BlossomInstitute.Domain.Entidades.Profesor;
using BlossomInstitute.Domain.Entidades.Usuario;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BlossomInstitute.Application.DataBase
{
    public interface IDataBaseService
    {
        DbSet<ProfesorEntity> Profesores { get; set; }

        // Identity (solo lectura para queries)
        IQueryable<UsuarioEntity> Usuarios { get; }
        IQueryable<IdentityRole<int>> Roles { get; }
        IQueryable<IdentityUserRole<int>> UserRoles { get; }

        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
        Task<bool> SaveAsync(CancellationToken ct = default);
    }
}

