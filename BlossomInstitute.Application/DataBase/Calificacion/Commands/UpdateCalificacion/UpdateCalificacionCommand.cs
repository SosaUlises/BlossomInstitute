using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Calificacion;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Calificacion.Commands.UpdateCalificacion
{
    public class UpdateCalificacionCommand : IUpdateCalificacionCommand
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public UpdateCalificacionCommand(
            IDataBaseService db,
            UserManager<UsuarioEntity> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int alumnoId,
            int calificacionId,
            int profesorUserId,
            UpdateCalificacionModel model,
            CancellationToken ct)
        {
            if (cursoId <= 0 || alumnoId <= 0 || calificacionId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Parámetros inválidos");

            if (model == null)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Modelo inválido");

            var profesor = await _userManager.FindByIdAsync(profesorUserId.ToString());
            if (profesor == null)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "No autenticado");

            if (!profesor.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Usuario inactivo");

            if (!await _userManager.IsInRoleAsync(profesor, "Profesor"))
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No autorizado");

            var profesorAsignado = await _db.CursoProfesores.AsNoTracking()
                .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);

            if (!profesorAsignado)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "Profesor no asignado a este curso");

            var calificacion = await _db.Calificaciones
                .Include(x => x.Detalles)
                .FirstOrDefaultAsync(x =>
                    x.Id == calificacionId &&
                    x.CursoId == cursoId &&
                    x.AlumnoId == alumnoId &&
                    !x.Archivado, ct);

            if (calificacion == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Calificación no encontrada");

            var detalles = model.Detalles?
                .Where(x => x != null)
                .ToList() ?? new List<UpdateCalificacionDetalleModel>();

            var tieneDetalles = detalles.Count > 0;

            var validacionReglas = ValidarReglasDeNegocio(model, detalles);
            if (validacionReglas != null)
                return validacionReglas;

            if (model.TareaId.HasValue)
            {
                var tareaValida = await _db.Tareas.AsNoTracking()
                    .AnyAsync(t => t.Id == model.TareaId.Value && t.CursoId == cursoId, ct);

                if (!tareaValida)
                    return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "La tarea no pertenece al curso");
            }

            if (model.EntregaId.HasValue)
            {
                var entrega = await _db.Entregas.AsNoTracking()
                    .Where(e => e.Id == model.EntregaId.Value)
                    .Select(e => new
                    {
                        e.Id,
                        e.AlumnoId,
                        e.TareaId,
                        CursoId = e.Tarea.CursoId
                    })
                    .FirstOrDefaultAsync(ct);

                if (entrega == null)
                    return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "La entrega no existe");

                if (entrega.AlumnoId != alumnoId)
                    return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "La entrega no corresponde al alumno");

                if (entrega.CursoId != cursoId)
                    return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "La entrega no pertenece al curso");

                if (model.TareaId.HasValue && entrega.TareaId != model.TareaId.Value)
                    return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "La entrega no corresponde a la tarea indicada");
            }

            if (model.Tipo == TipoCalificacion.Homework && model.TareaId.HasValue && model.EntregaId.HasValue)
            {
                var existeOtraHomework = await _db.Calificaciones.AsNoTracking()
                    .AnyAsync(x =>
                        x.Id != calificacionId &&
                        x.CursoId == cursoId &&
                        x.AlumnoId == alumnoId &&
                        x.TareaId == model.TareaId &&
                        x.EntregaId == model.EntregaId &&
                        !x.Archivado, ct);

                if (existeOtraHomework)
                    return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "Ya existe otra calificación activa para esa tarea y entrega");
            }

            decimal notaFinal;
            if (tieneDetalles)
            {
                notaFinal = CalcularNotaDesdeDetalles(detalles);
            }
            else
            {
                notaFinal = model.Nota!.Value;
            }

            await using var tx = await _db.BeginTransactionAsync(ct);

            try
            {
                calificacion.Tipo = model.Tipo;
                calificacion.Titulo = model.Titulo.Trim();
                calificacion.Descripcion = string.IsNullOrWhiteSpace(model.Descripcion) ? null : model.Descripcion.Trim();
                calificacion.Nota = Math.Round(notaFinal, 2);
                calificacion.Fecha = model.Fecha;
                calificacion.TareaId = model.TareaId;
                calificacion.EntregaId = model.EntregaId;
                calificacion.TieneDetalleSkills = tieneDetalles;
                calificacion.UpdatedAtUtc = DateTime.UtcNow;

                if (calificacion.Detalles.Any())
                    _db.CalificacionDetalles.RemoveRange(calificacion.Detalles);

                if (tieneDetalles)
                {
                    var nuevosDetalles = detalles
                        .Select(d => new CalificacionDetalleEntity
                        {
                            CalificacionId = calificacion.Id,
                            Skill = d.Skill,
                            PuntajeObtenido = d.PuntajeObtenido,
                            PuntajeMaximo = d.PuntajeMaximo
                        })
                        .ToList();

                    await _db.CalificacionDetalles.AddRangeAsync(nuevosDetalles, ct);
                }

                var ok = await _db.SaveAsync(ct);
                if (!ok)
                {
                    await tx.RollbackAsync(ct);
                    return ResponseApiService.Response(StatusCodes.Status500InternalServerError, message: "No se pudo actualizar la calificación");
                }

                await tx.CommitAsync(ct);

                var detallesResponse = tieneDetalles
                    ? detalles.Select(d => new
                    {
                        d.Skill,
                        d.PuntajeObtenido,
                        d.PuntajeMaximo
                    })
                    : Enumerable.Empty<object>();

                return ResponseApiService.Response(StatusCodes.Status200OK, new
                {
                    calificacion.Id,
                    calificacion.CursoId,
                    calificacion.AlumnoId,
                    calificacion.Tipo,
                    calificacion.Titulo,
                    calificacion.Nota,
                    calificacion.Fecha,
                    calificacion.TieneDetalleSkills,
                    detalles = detallesResponse
                }, "Calificación actualizada correctamente");
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync(ct);
                return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "No se pudo actualizar la calificación por conflicto de datos");
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        private BaseResponseModel? ValidarReglasDeNegocio(
            UpdateCalificacionModel model,
            List<UpdateCalificacionDetalleModel> detalles)
        {
            if (string.IsNullOrWhiteSpace(model.Titulo))
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "El título es obligatorio");

            if (model.Titulo.Trim().Length > 100)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "El título no puede superar los 100 caracteres");

            if (!string.IsNullOrWhiteSpace(model.Descripcion) && model.Descripcion.Trim().Length > 500)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "La descripción no puede superar los 500 caracteres");

            var tieneDetalles = detalles.Count > 0;

            if (model.Tipo == TipoCalificacion.Test && !tieneDetalles)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Los tests deben incluir detalle por skills");

            if ((model.Tipo == TipoCalificacion.Participation || model.Tipo == TipoCalificacion.Behaviour) &&
                (model.TareaId.HasValue || model.EntregaId.HasValue))
            {
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Participation y Behaviour no pueden vincularse a tarea o entrega");
            }

            if ((model.Tipo == TipoCalificacion.Participation || model.Tipo == TipoCalificacion.Behaviour) && tieneDetalles)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Participation y Behaviour no admiten detalle por skills");

            if (model.EntregaId.HasValue && !model.TareaId.HasValue && model.Tipo == TipoCalificacion.Homework)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Si se informa una entrega de homework, también debe informarse la tarea");

            if (tieneDetalles)
            {
                var skillsDuplicadas = detalles
                    .GroupBy(x => x.Skill)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (skillsDuplicadas.Count > 0)
                    return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "No se puede repetir la misma skill dentro de una misma calificación");

                if (detalles.Any(x => x.PuntajeMaximo <= 0))
                    return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "El puntaje máximo debe ser mayor a cero");

                if (detalles.Any(x => x.PuntajeObtenido < 0))
                    return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "El puntaje obtenido no puede ser negativo");

                if (detalles.Any(x => x.PuntajeObtenido > x.PuntajeMaximo))
                    return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "El puntaje obtenido no puede superar el puntaje máximo");
            }
            else
            {
                if (!model.Nota.HasValue)
                    return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Debe informar la nota cuando no se cargan detalles");

                if (model.Nota.Value < 0 || model.Nota.Value > 100)
                    return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "La nota debe estar entre 0 y 100");
            }

            return null;
        }

        private static decimal CalcularNotaDesdeDetalles(List<UpdateCalificacionDetalleModel> detalles)
        {
            var totalObtenido = detalles.Sum(x => x.PuntajeObtenido);
            var totalMaximo = detalles.Sum(x => x.PuntajeMaximo);

            if (totalMaximo <= 0)
                throw new InvalidOperationException("No se puede calcular la nota con puntaje máximo total menor o igual a cero");

            return (totalObtenido / totalMaximo) * 100m;
        }
    }
}

