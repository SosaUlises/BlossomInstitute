using BlossomInstitute.Application.DataBase;
using BlossomInstitute.Domain.Entidades.Profesor;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Infraestructure.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BlossomInstitute.Infraestructure.DataBase
{
    public class DataBaseService
         : IdentityDbContext<UsuarioEntity, IdentityRole<int>, int>, IDataBaseService
    {
        public DataBaseService(DbContextOptions<DataBaseService> options) : base(options)
        {
        }

        public DbSet<ProfesorEntity> Profesores { get; set; }

        // Identity (solo lectura para queries)
        public IQueryable<UsuarioEntity> Usuarios => Users.AsNoTracking();
        public IQueryable<IdentityRole<int>> Roles => base.Roles.AsNoTracking();
        public IQueryable<IdentityUserRole<int>> UserRoles => base.UserRoles.AsNoTracking();

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        => Database.BeginTransactionAsync(ct);

        public async Task<bool> SaveAsync(CancellationToken ct = default)
            => await base.SaveChangesAsync(ct) > 0;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            EntityConfiguration(modelBuilder);
        }

        private void EntityConfiguration(ModelBuilder modelBuilder)
        {
            new ProfesorConfiguration(modelBuilder.Entity<ProfesorEntity>());
        }

    }
}
