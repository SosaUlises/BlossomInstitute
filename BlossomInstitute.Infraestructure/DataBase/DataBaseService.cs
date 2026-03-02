using BlossomInstitute.Application.DataBase;
using BlossomInstitute.Domain.Entidades.Alumno;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Entidades.Profesor;
using BlossomInstitute.Domain.Entidades.Tarea;
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

        public DbSet<ClaseEntity> Clases { get; set; }
        public DbSet<AsistenciaEntity> Asistencias { get; set; }
        public DbSet<ProfesorEntity> Profesores { get; set; }
        public DbSet<AlumnoEntity> Alumnos { get; set; }
        public DbSet<CursoEntity> Cursos { get; set; }
        public DbSet<CursoHorarioEntity> CursoHorarios { get; set; }
        public DbSet<CursoProfesorEntity> CursoProfesores { get; set; }
        public DbSet<MatriculaEntity> Matriculas { get; set; }
        public DbSet<TareaEntity> Tareas { get; set; }
        public DbSet<TareaRecursoEntity> TareaRecursos { get; set; }

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
            new AlumnoConfiguration(modelBuilder.Entity<AlumnoEntity>());
            new CursoConfiguration(modelBuilder.Entity<CursoEntity>());
            new MatriculaConfiguration(modelBuilder.Entity<MatriculaEntity>());
            new CursoHorarioConfiguration(modelBuilder.Entity<CursoHorarioEntity>());
            new CursoProfesorConfiguration(modelBuilder.Entity<CursoProfesorEntity>());
            new TareaConfiguration(modelBuilder.Entity<TareaEntity>());
            new TareaRecursoConfiguration(modelBuilder.Entity<TareaRecursoEntity>());
        }

    }
}
