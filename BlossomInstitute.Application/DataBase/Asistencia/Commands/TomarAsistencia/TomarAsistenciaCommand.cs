using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Asistencia.Commands.TomarAsistencia
{
    public class TomarAsistenciaCommand : ITomarAsistenciaCommand
    {
        private readonly IDataBaseService _db;

        public TomarAsistenciaCommand(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            DateOnly fecha,
            TomarAsistenciaModel? model,
            CancellationToken ct = default)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "CursoId invalido");

            if (model == null)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "El cuerpo de la solicitud es obligatorio");

            if (model.Asistencias == null || model.Asistencias.Count == 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Debe enviar asistencias");

            var asistenciasSolicitadas = model.Asistencias;
            var alumnoIdsDuplicados = asistenciasSolicitadas
                .GroupBy(x => x.AlumnoId)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

            if (alumnoIdsDuplicados.Count > 0)
            {
                return ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    new { alumnoIdsDuplicados },
                    "No puede repetir alumnos en la misma toma de asistencia");
            }

            var curso = await _db.Cursos.FirstOrDefaultAsync(c => c.Id == cursoId, ct);
            if (curso == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Curso no encontrado");

            if (curso.Estado == EstadoCurso.Inactivo || curso.Estado == EstadoCurso.Archivado)
                return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "No se puede tomar asistencia en un curso inactivo/archivado");

            var alumnoIds = asistenciasSolicitadas.Select(x => x.AlumnoId).Distinct().ToList();
            if (alumnoIds.Any(id => id <= 0))
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "AlumnoId invalido");

            var matriculados = await _db.Matriculas
                .Where(m => m.CursoId == cursoId && alumnoIds.Contains(m.AlumnoId))
                .Select(m => m.AlumnoId)
                .ToListAsync(ct);

            var noMatriculados = alumnoIds.Except(matriculados).ToList();
            if (noMatriculados.Count > 0)
            {
                return ResponseApiService.Response(
                    StatusCodes.Status409Conflict,
                    new { alumnoIdsNoMatriculados = noMatriculados },
                    "Hay alumnos no matriculados en el curso");
            }

            await using var transaccion = await _db.BeginTransactionAsync(ct);

            try
            {
                var clase = await _db.Clases
                    .FirstOrDefaultAsync(x => x.CursoId == cursoId && x.Fecha == fecha, ct);

                if (clase == null)
                {
                    clase = new ClaseEntity
                    {
                        CursoId = cursoId,
                        Fecha = fecha,
                        Estado = EstadoClase.Programada,
                        Descripcion = model.DescripcionClase
                    };

                    _db.Clases.Add(clase);
                    await _db.SaveAsync(ct);
                }
                else
                {
                    if (clase.Estado == EstadoClase.Cancelada)
                    {
                        await transaccion.RollbackAsync(ct);
                        return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "La clase esta cancelada. No se puede cargar asistencia.");
                    }

                    if (!string.IsNullOrWhiteSpace(model.DescripcionClase))
                        clase.Descripcion = model.DescripcionClase;
                }

                var existentes = await _db.Asistencias
                    .Where(a => a.ClaseId == clase.Id && alumnoIds.Contains(a.AlumnoId))
                    .ToListAsync(ct);

                var existentesPorAlumnoId = existentes.ToDictionary(x => x.AlumnoId, x => x);

                var insertados = 0;
                var actualizados = 0;

                foreach (var item in asistenciasSolicitadas)
                {
                    if (existentesPorAlumnoId.TryGetValue(item.AlumnoId, out var asistenciaExistente))
                    {
                        if (asistenciaExistente.Estado != item.Estado)
                        {
                            asistenciaExistente.Estado = item.Estado;
                            actualizados++;
                        }
                    }
                    else
                    {
                        _db.Asistencias.Add(new AsistenciaEntity
                        {
                            ClaseId = clase.Id,
                            AlumnoId = item.AlumnoId,
                            Estado = item.Estado
                        });
                        insertados++;
                    }
                }

                await _db.SaveAsync(ct);
                await transaccion.CommitAsync(ct);

                return ResponseApiService.Response(StatusCodes.Status200OK, new
                {
                    cursoId,
                    fecha = fecha.ToString("yyyy-MM-dd"),
                    claseId = clase.Id,
                    insertados,
                    actualizados
                }, "Asistencia guardada");
            }
            catch (DbUpdateException)
            {
                await transaccion.RollbackAsync(ct);
                return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "No se pudo guardar la asistencia por conflicto de datos");
            }
            catch
            {
                await transaccion.RollbackAsync(ct);
                throw;
            }
        }
    }
}
