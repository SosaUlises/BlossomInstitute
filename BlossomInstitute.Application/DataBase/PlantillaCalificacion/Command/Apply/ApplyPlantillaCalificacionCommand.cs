using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Calificacion;
using BlossomInstitute.Domain.Entidades.Calificaciones;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Command.Apply
{
    public class ApplyPlantillaCalificacionCommand : IApplyPlantillaCalificacionCommand
    {
        private readonly IDataBaseService _db;
        private readonly UserManager<UsuarioEntity> _userManager;

        public ApplyPlantillaCalificacionCommand(
            IDataBaseService db,
            UserManager<UsuarioEntity> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int plantillaId,
            int profesorUserId,
            ApplyPlantillaCalificacionModel model,
            CancellationToken ct)
        {
            if (cursoId <= 0 || plantillaId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "Parámetros inválidos");

            if (model == null)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "Modelo inválido");

            var profesor = await _userManager.FindByIdAsync(profesorUserId.ToString());
            if (profesor == null)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, "No autenticado");

            if (!profesor.Activo)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Usuario inactivo");

            if (!await _userManager.IsInRoleAsync(profesor, "Profesor"))
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "No autorizado");

            var cursoExiste = await _db.Cursos
                .AsNoTracking()
                .AnyAsync(c => c.Id == cursoId, ct);

            if (!cursoExiste)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Curso no encontrado");

            var profesorAsignado = await _db.CursoProfesores
                .AsNoTracking()
                .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == profesorUserId, ct);

            if (!profesorAsignado)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, "Profesor no asignado a este curso");

            var plantilla = await _db.PlantillaCalificaciones
                .AsNoTracking()
                .Where(x =>
                    x.Id == plantillaId &&
                    x.CursoId == cursoId &&
                    !x.Archivada)
                .Select(x => new
                {
                    x.Id,
                    x.CursoId,
                    x.Tipo,
                    x.Titulo,
                    x.Descripcion,
                    Detalles = x.Detalles
                        .Select(d => new
                        {
                            d.Skill,
                            d.PuntajeMaximo
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(ct);

            if (plantilla == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, "Plantilla no encontrada");

            if (plantilla.Tipo != TipoCalificacion.Quiz && plantilla.Tipo != TipoCalificacion.Test)
                return ResponseApiService.Response(StatusCodes.Status409Conflict, "Solo se pueden aplicar plantillas de tipo Quiz o Test");

            if (model.Alumnos == null || model.Alumnos.Count == 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "Debe informar al menos un alumno");

            var plantillaSkills = plantilla.Detalles
                .OrderBy(x => (int)x.Skill)
                .ToList();

            if (plantillaSkills.Count == 0)
                return ResponseApiService.Response(StatusCodes.Status409Conflict, "La plantilla no tiene skills configuradas");

            var alumnosIds = model.Alumnos
                .Select(x => x.AlumnoId)
                .ToList();

            if (alumnosIds.Any(x => x <= 0))
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "Hay alumnos inválidos en la solicitud");

            var alumnosDuplicados = alumnosIds
                .GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (alumnosDuplicados.Count > 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, "No se puede repetir el mismo alumno dentro de la misma aplicación");

            var alumnosMatriculados = await _db.Matriculas
                .AsNoTracking()
                .Where(x => x.CursoId == cursoId && alumnosIds.Contains(x.AlumnoId))
                .Select(x => x.AlumnoId)
                .ToListAsync(ct);

            var alumnosNoMatriculados = alumnosIds
                .Except(alumnosMatriculados)
                .ToList();

            if (alumnosNoMatriculados.Count > 0)
                return ResponseApiService.Response(StatusCodes.Status409Conflict, "Hay alumnos que no están matriculados en el curso");

            foreach (var alumno in model.Alumnos)
            {
                if (alumno.Detalles == null || alumno.Detalles.Count == 0)
                    return ResponseApiService.Response(
                        StatusCodes.Status400BadRequest,
                        $"El alumno {alumno.AlumnoId} no tiene detalles cargados");

                var repeatedSkills = alumno.Detalles
                    .GroupBy(x => x.Skill)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (repeatedSkills.Count > 0)
                    return ResponseApiService.Response(
                        StatusCodes.Status400BadRequest,
                        $"El alumno {alumno.AlumnoId} tiene skills repetidas");

                var alumnoSkills = alumno.Detalles
                    .Select(x => x.Skill)
                    .OrderBy(x => (int)x)
                    .ToList();

                var expectedSkills = plantillaSkills
                    .Select(x => x.Skill)
                    .OrderBy(x => (int)x)
                    .ToList();

                if (alumnoSkills.Count != expectedSkills.Count || !alumnoSkills.SequenceEqual(expectedSkills))
                {
                    return ResponseApiService.Response(
                        StatusCodes.Status400BadRequest,
                        $"El alumno {alumno.AlumnoId} no coincide con las skills definidas en la plantilla");
                }

                foreach (var detalle in alumno.Detalles)
                {
                    var plantillaDetalle = plantillaSkills.First(x => x.Skill == detalle.Skill);

                    if (plantillaDetalle.PuntajeMaximo <= 0)
                        return ResponseApiService.Response(
                            StatusCodes.Status409Conflict,
                            "La plantilla contiene un puntaje máximo inválido");

                    if (detalle.PuntajeObtenido < 0)
                        return ResponseApiService.Response(
                            StatusCodes.Status400BadRequest,
                            $"El alumno {alumno.AlumnoId} tiene puntajes obtenidos negativos");

                    if (detalle.PuntajeObtenido > plantillaDetalle.PuntajeMaximo)
                        return ResponseApiService.Response(
                            StatusCodes.Status400BadRequest,
                            $"El alumno {alumno.AlumnoId} tiene un puntaje obtenido mayor al máximo permitido en la skill {detalle.Skill}");
                }
            }

            var calificaciones = new List<CalificacionEntity>();

            foreach (var alumno in model.Alumnos)
            {
                var detallesCalificacion = plantillaSkills
                    .Select(skillPlantilla =>
                    {
                        var detalleAlumno = alumno.Detalles.First(x => x.Skill == skillPlantilla.Skill);

                        return new CalificacionDetalleEntity
                        {
                            Skill = skillPlantilla.Skill,
                            PuntajeObtenido = detalleAlumno.PuntajeObtenido,
                            PuntajeMaximo = skillPlantilla.PuntajeMaximo
                        };
                    })
                    .ToList();

                var notaFinal = CalcularNotaDesdeDetalles(detallesCalificacion);

                var calificacion = new CalificacionEntity
                {
                    CursoId = cursoId,
                    AlumnoId = alumno.AlumnoId,
                    Tipo = plantilla.Tipo,
                    Titulo = plantilla.Titulo.Trim(),
                    Descripcion = string.IsNullOrWhiteSpace(plantilla.Descripcion)
                        ? null
                        : plantilla.Descripcion.Trim(),
                    Nota = Math.Round(notaFinal, 2),
                    Fecha = model.Fecha,
                    TareaId = null,
                    EntregaId = null,
                    TieneDetalleSkills = true,
                    Archivado = false,
                    ArchivadoPorTarea = false,
                    CreatedAtUtc = DateTime.UtcNow,
                    Detalles = detallesCalificacion
                };

                calificaciones.Add(calificacion);
            }

            await using var tx = await _db.BeginTransactionAsync(ct);

            try
            {
                _db.Calificaciones.AddRange(calificaciones);

                var ok = await _db.SaveAsync(ct);
                if (!ok)
                {
                    await tx.RollbackAsync(ct);
                    return ResponseApiService.Response(StatusCodes.Status500InternalServerError, "No se pudieron guardar las calificaciones");
                }

                await tx.CommitAsync(ct);

                return ResponseApiService.Response(StatusCodes.Status201Created, new
                {
                    plantillaId = plantilla.Id,
                    cursoId,
                    totalCalificacionesCreadas = calificaciones.Count,
                    items = calificaciones.Select(x => new
                    {
                        x.Id,
                        x.AlumnoId,
                        x.Tipo,
                        x.Titulo,
                        x.Nota,
                        x.Fecha
                    }).ToList()
                }, "Plantilla aplicada correctamente");
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync(ct);
                return ResponseApiService.Response(StatusCodes.Status409Conflict, "No se pudieron guardar las calificaciones por conflicto de datos");
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        private static decimal CalcularNotaDesdeDetalles(List<CalificacionDetalleEntity> detalles)
        {
            var totalObtenido = detalles.Sum(x => x.PuntajeObtenido);
            var totalMaximo = detalles.Sum(x => x.PuntajeMaximo);

            if (totalMaximo <= 0)
                throw new InvalidOperationException("No se puede calcular la nota con puntaje máximo total menor o igual a cero");

            return (totalObtenido / totalMaximo) * 100m;
        }
    }
}

