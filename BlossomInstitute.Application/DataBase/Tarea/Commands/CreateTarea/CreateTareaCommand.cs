using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Entidades.Tarea;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Tarea.Commands.CreateTarea
{
    public class CreateTareaCommand : ICreateTareaCommand
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public CreateTareaCommand(IDataBaseService db, UserManager<UsuarioEntity> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int profesorUserId,
            CreateTareaModel model,
            CancellationToken ct = default)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "CursoId inválido");

            if (profesorUserId <= 0)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, "No autenticado");

            var user = await _userManager.FindByIdAsync(profesorUserId.ToString());
            if (user == null)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, "No autenticado");

            if (!user.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Usuario inválido o inactivo");

            var isProfesor = await _userManager.IsInRoleAsync(user, "Profesor");
            var isAdmin = await _userManager.IsInRoleAsync(user, "Administrador");

            if (!isProfesor && !isAdmin)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Acceso denegado");

            var curso = await _db.Cursos
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == cursoId, ct);

            if (curso == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Curso no encontrado");

            if (curso.Estado != EstadoCurso.Activo)
                return ResponseApiService.Response(StatusCodes.Status409Conflict, "El curso no se encuentra activo");

            if (!isAdmin)
            {
                var profesorAsignado = await _db.CursoProfesores
                    .AsNoTracking()
                    .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);

                if (!profesorAsignado)
                    return ResponseApiService.Response(StatusCodes.Status403Forbidden, "No estás asignado a este curso");
            }

            var recursos = model.Recursos ?? new List<CreateTareaRecursoModel>();

            recursos = recursos
                .Where(r => !string.IsNullOrWhiteSpace(r.Url))
                .GroupBy(r =>
                    !string.IsNullOrWhiteSpace(r.StorageKey)
                        ? r.StorageKey!.Trim().ToLowerInvariant()
                        : r.Url!.Trim().ToLowerInvariant())
                .Select(g => g.First())
                .ToList();

            await using var tx = await _db.BeginTransactionAsync(ct);

            try
            {
                var nowUtc = DateTime.UtcNow;

                var tarea = new TareaEntity
                {
                    CursoId = cursoId,
                    ProfesorId = profesorUserId,
                    Titulo = model.Titulo.Trim(),
                    Consigna = string.IsNullOrWhiteSpace(model.Consigna) ? null : model.Consigna.Trim(),
                    FechaEntregaUtc = model.FechaEntregaUtc,
                    Estado = model.Estado,
                    CreatedAtUtc = nowUtc
                };

                foreach (var r in recursos)
                {
                    tarea.Recursos.Add(new TareaRecursoEntity
                    {
                        Tipo = r.Tipo,
                        Url = r.Url!.Trim(),
                        Nombre = string.IsNullOrWhiteSpace(r.Nombre) ? null : r.Nombre.Trim(),
                        StorageProvider = r.StorageProvider,
                        StorageKey = string.IsNullOrWhiteSpace(r.StorageKey) ? null : r.StorageKey.Trim(),
                        ContentType = string.IsNullOrWhiteSpace(r.ContentType) ? null : r.ContentType.Trim(),
                        SizeBytes = r.SizeBytes
                    });
                }

                _db.Tareas.Add(tarea);

                var ok = await _db.SaveAsync(ct);
                if (!ok)
                {
                    await tx.RollbackAsync(ct);
                    return ResponseApiService.Response(StatusCodes.Status500InternalServerError, "No se pudo crear la tarea");
                }

                await tx.CommitAsync(ct);

                return ResponseApiService.Response(StatusCodes.Status201Created, new
                {
                    tarea.Id,
                    tarea.CursoId,
                    tarea.ProfesorId,
                    tarea.Titulo,
                    tarea.Estado,
                    tarea.FechaEntregaUtc,
                    EsAnuncio = !tarea.FechaEntregaUtc.HasValue
                }, "Tarea creada correctamente");
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
    }
}




