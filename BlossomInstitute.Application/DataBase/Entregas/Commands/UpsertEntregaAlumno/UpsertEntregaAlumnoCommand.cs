using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Entidades.Entrega;
using BlossomInstitute.Domain.Entidades.Tarea;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Entregas.Commands.UpsertEntregaAlumno
{
    public class UpsertEntregaAlumnoCommand : IUpsertEntregaAlumnoCommand
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public UpsertEntregaAlumnoCommand(IDataBaseService db, UserManager<UsuarioEntity> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(int tareaId, int userId, UpsertEntregaAlumnoModel model, CancellationToken ct)
        {
            if (tareaId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "TareaId inválido");

            // Usuario + rol Alumno + activo
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, "No autenticado");

            if (!user.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Usuario inactivo");

            if (!await _userManager.IsInRoleAsync(user, "Alumno"))
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "No autorizado");

            var alumnoId = user.Id;

            // Validar tarea existe + publicada
            var tarea = await _db.Tareas
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tareaId, ct);

            if (tarea == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Tarea no encontrada");

            if (tarea.Estado == EstadoTarea.Archivada)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Tarea archivada");

            if (tarea.Estado != EstadoTarea.Publicada)
                return ResponseApiService.Response(StatusCodes.Status409Conflict, "La tarea no está publicada");

            // Validar matricula
            var estaMatriculado = await _db.Matriculas
                .AsNoTracking()
                .AnyAsync(m => m.CursoId == tarea.CursoId && m.AlumnoId == alumnoId, ct);

            if (!estaMatriculado)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "No estás matriculado en este curso");

            // Estado entrega (en termino / fuera de termino)
            DateTime nowUtc = DateTime.UtcNow;
            DateTime? deadlineUtc = tarea.FechaEntregaUtc;

            var estadoEntrega = (deadlineUtc.HasValue && nowUtc > deadlineUtc.Value)
                ? EstadoEntrega.FueraDeTermino
                : EstadoEntrega.EntregadaEnTermino;

            // Sanitizar adjuntos (evitar nulls)
            var adjuntos = model.Adjuntos ?? new List<UpsertEntregaAdjuntoModel>();
            // Eliminar duplicados por URL
            adjuntos = adjuntos
                .Where(a => !string.IsNullOrWhiteSpace(a.Url))
                .GroupBy(a => a.Url.Trim())
                .Select(g => g.First())
                .ToList();

            await using var tx = await _db.BeginTransactionAsync(ct);

            // Upsert: buscar entrega existente (única por tarea+alumno)
            var entrega = await _db.Entregas
                .Include(e => e.Adjuntos)
                .FirstOrDefaultAsync(e => e.TareaId == tareaId && e.AlumnoId == alumnoId, ct);

            var created = false;

            if (entrega == null)
            {
                created = true;

                entrega = new EntregaEntity
                {
                    TareaId = tareaId,
                    AlumnoId = alumnoId,
                    Texto = string.IsNullOrWhiteSpace(model.Texto) ? null : model.Texto.Trim(),
                    FechaEntregaUtc = nowUtc,
                    Estado = estadoEntrega,
                    CreatedAtUtc = nowUtc
                };

                _db.Entregas.Add(entrega);
                // Guardar para tener Entrega.Id y poder adjuntar
                var ok = await _db.SaveAsync(ct);
                if (!ok)
                {
                    await tx.RollbackAsync(ct);
                    return ResponseApiService.Response(StatusCodes.Status500InternalServerError, "No se pudo registrar la entrega");
                }

                // Adjuntos
                foreach (var a in adjuntos)
                {
                    _db.EntregaAdjuntos.Add(new EntregaAdjuntoEntity
                    {
                        EntregaId = entrega.Id,
                        Tipo = a.Tipo,
                        Url = a.Url.Trim(),
                        Nombre = string.IsNullOrWhiteSpace(a.Nombre) ? null : a.Nombre.Trim()
                    });
                }
            }
            else
            {
                // Update (re-entrega)
                entrega.Texto = string.IsNullOrWhiteSpace(model.Texto) ? null : model.Texto.Trim();
                entrega.FechaEntregaUtc = nowUtc;
                entrega.Estado = estadoEntrega;
                entrega.UpdatedAtUtc = nowUtc;

                // Reemplazar adjuntos
                if (entrega.Adjuntos != null && entrega.Adjuntos.Count > 0)
                {
                    _db.EntregaAdjuntos.RemoveRange(entrega.Adjuntos);
                }

                foreach (var a in adjuntos)
                {
                    _db.EntregaAdjuntos.Add(new EntregaAdjuntoEntity
                    {
                        EntregaId = entrega.Id,
                        Tipo = a.Tipo,
                        Url = a.Url.Trim(),
                        Nombre = string.IsNullOrWhiteSpace(a.Nombre) ? null : a.Nombre.Trim()
                    });
                }
            }

            try
            {
                var saved = await _db.SaveAsync(ct);
                if (!saved)
                {
                    await tx.RollbackAsync(ct);
                    return ResponseApiService.Response(StatusCodes.Status500InternalServerError, "No se pudo guardar la entrega");
                }

                await tx.CommitAsync(ct);

                return ResponseApiService.Response(
                    created ? StatusCodes.Status201Created : StatusCodes.Status200OK,
                    new
                    {
                        entrega.Id,
                        entrega.TareaId,
                        entrega.AlumnoId,
                        entrega.Estado,
                        entrega.FechaEntregaUtc,
                        created
                    },
                    created ? "Entrega creada correctamente" : "Entrega actualizada correctamente"
                );
            }
            catch (DbUpdateException ex)
            {
                await tx.RollbackAsync(ct);

                return ResponseApiService.Response(
                    StatusCodes.Status409Conflict,
                    ex.Message,
                    "Conflicto al guardar la entrega. Intentá nuevamente."
                );
            }
        }
    }
}
