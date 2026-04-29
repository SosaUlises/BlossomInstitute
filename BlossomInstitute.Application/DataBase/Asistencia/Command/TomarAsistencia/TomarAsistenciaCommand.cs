using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Asistencia.Command.TomarAsistencia
{
    public class TomarAsistenciaCommand : ITomarAsistenciaCommand
    {
        private readonly IDataBaseService _db;

        public TomarAsistenciaCommand(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int cursoId, DateOnly fecha, TomarAsistenciaModel model, CancellationToken ct = default)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "CursoId inválido");

            if (model.Asistencias == null || model.Asistencias.Count == 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Debe enviar asistencias");

            var curso = await _db.Cursos.FirstOrDefaultAsync(c => c.Id == cursoId, ct);
            if (curso == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Curso no encontrado");

            if (curso.Estado == EstadoCurso.Inactivo || curso.Estado == EstadoCurso.Archivado)
                return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "No se puede tomar asistencia en un curso inactivo/archivado");

            var alumnoIds = model.Asistencias.Select(x => x.AlumnoId).Distinct().ToList();
            if (alumnoIds.Any(id => id <= 0))
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "AlumnoId inválido");

            // validar que estén matriculados en el curso
            var matriculados = await _db.Matriculas
                .Where(m => m.CursoId == cursoId && alumnoIds.Contains(m.AlumnoId))
                .Select(m => m.AlumnoId)
                .ToListAsync(ct);

            var noMatriculados = alumnoIds.Except(matriculados).ToList();
            if (noMatriculados.Count > 0)
                return ResponseApiService.Response(StatusCodes.Status409Conflict,
                    new { alumnoIdsNoMatriculados = noMatriculados },
                    "Hay alumnos no matriculados en el curso");

            await using var tx = await _db.BeginTransactionAsync(ct);

            // buscar o crear clase
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
                    await tx.RollbackAsync(ct);
                    return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "La clase está cancelada. No se puede cargar asistencia.");
                }

                // actualizar descripción si viene
                if (!string.IsNullOrWhiteSpace(model.DescripcionClase))
                    clase.Descripcion = model.DescripcionClase;
            }

            // upsert asistencias
            var existentes = await _db.Asistencias
                .Where(a => a.ClaseId == clase.Id && alumnoIds.Contains(a.AlumnoId))
                .ToListAsync(ct);

            var existentesByAlumnoId = existentes.ToDictionary(x => x.AlumnoId, x => x);

            int insertados = 0;
            int actualizados = 0;

            foreach (var item in model.Asistencias)
            {
                if (existentesByAlumnoId.TryGetValue(item.AlumnoId, out var asis))
                {
                    if (asis.Estado != item.Estado)
                    {
                        asis.Estado = item.Estado;
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
            await tx.CommitAsync(ct);

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                cursoId,
                fecha = fecha.ToString("yyyy-MM-dd"),
                claseId = clase.Id,
                insertados,
                actualizados
            }, "Asistencia guardada");
        }
    }
}
