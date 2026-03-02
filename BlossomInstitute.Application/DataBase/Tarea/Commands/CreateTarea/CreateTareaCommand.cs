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

        public async Task<BaseResponseModel> Execute(int cursoId, int profesorUserId, CreateTareaModel model, CancellationToken ct = default)
        {
            if (cursoId <= 0) return ResponseApiService.Response(400, "CursoId inválido");
            if (profesorUserId <= 0) return ResponseApiService.Response(401, "No autenticado");

            var user = await _userManager.FindByIdAsync(profesorUserId.ToString());
            if (user == null || !user.Activo) return ResponseApiService.Response(403, "Usuario inválido o inactivo");
            if (!await _userManager.IsInRoleAsync(user, "Profesor")) return ResponseApiService.Response(403, "Acceso denegado");

            var curso = await _db.Cursos.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cursoId, ct);
            if (curso == null) return ResponseApiService.Response(404, "Curso no encontrado");

            if (curso.Estado != EstadoCurso.Activo) return ResponseApiService.Response(404, "Curso no se encuentra activo");

            // Profesor asignado al curso
            var profesorAsignado = await _db.CursoProfesores.AsNoTracking()
                .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);
            if (!profesorAsignado)
                return ResponseApiService.Response(403, "No estás asignado a este curso");

            await using var tx = await _db.BeginTransactionAsync(ct);

            var tarea = new TareaEntity
            {
                CursoId = cursoId,
                ProfesorId = profesorUserId,
                Titulo = model.Titulo.Trim(),
                Consigna = model.Consigna?.Trim(),
                FechaEntregaUtc = model.FechaEntregaUtc,
                Estado = EstadoTarea.Publicada,
                CreatedAtUtc = DateTime.UtcNow
            };

            if (model.Recursos != null && model.Recursos.Count > 0)
            {
                foreach (var r in model.Recursos)
                {
                    tarea.Recursos.Add(new TareaRecursoEntity
                    {
                        Tipo = r.Tipo,
                        Url = r.Url.Trim(),
                        Nombre = r.Nombre?.Trim()
                    });
                }
            }

            _db.Tareas.Add(tarea);
            await _db.SaveAsync(ct);

            await tx.CommitAsync(ct);

            return ResponseApiService.Response(StatusCodes.Status201Created, new
            {
                tarea.Id,
                tarea.CursoId,
                tarea.ProfesorId,
                tarea.Titulo,
                tarea.Estado,
                tarea.FechaEntregaUtc
            }, "Tarea creada correctamente");
        }
    }
}
