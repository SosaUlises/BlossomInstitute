using BlossomInstitute.Application.DataBase;
using BlossomInstitute.Domain.Entidades.Usuario;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Persistence.DataBase
{
    public class DataBaseService
        : IdentityDbContext<UsuarioEntity, IdentityRole<int>, int>, IDataBaseService
    {
        public DataBaseService(DbContextOptions<DataBaseService> options) : base(options)
        {
        }

        public async Task<bool> SaveAsync(CancellationToken ct = default)
            => await base.SaveChangesAsync(ct) > 0;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
