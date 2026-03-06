using BlossomInstitute.Domain.Entidades.Alumno;
using BlossomInstitute.Domain.Entidades.Calificaciones;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Entidades.Entrega;
using BlossomInstitute.Domain.Entidades.Profesor;
using BlossomInstitute.Domain.Entidades.Tarea;
using BlossomInstitute.Domain.Entidades.Usuario;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BlossomInstitute.Application.DataBase
{
    public interface IDataBaseService
    {
        DbSet<ProfesorEntity> Profesores { get; set; }
        DbSet<AlumnoEntity> Alumnos { get; set; }
        DbSet<CursoEntity> Cursos { get; set; }
        DbSet<CursoHorarioEntity> CursoHorarios { get; set; }
        DbSet<CursoProfesorEntity> CursoProfesores { get; set; }
        DbSet<MatriculaEntity> Matriculas { get; set; }
        DbSet<ClaseEntity> Clases { get; set; }
        DbSet<AsistenciaEntity> Asistencias { get; set; }
        DbSet<TareaEntity> Tareas { get; set; }
        DbSet<TareaRecursoEntity> TareaRecursos { get; set; }
        DbSet<EntregaEntity> Entregas { get; set; }
        DbSet<EntregaAdjuntoEntity> EntregaAdjuntos { get; set; }
        DbSet<FeedbackEntregaEntity> EntregaFeedbacks { get; set; }
        DbSet<CalificacionEntity> Calificaciones { get; set; }


        // Identity (solo lectura para queries)
        IQueryable<UsuarioEntity> Usuarios { get; }
        IQueryable<IdentityRole<int>> Roles { get; }
        IQueryable<IdentityUserRole<int>> UserRoles { get; }

        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
        Task<bool> SaveAsync(CancellationToken ct = default);
    }
}

