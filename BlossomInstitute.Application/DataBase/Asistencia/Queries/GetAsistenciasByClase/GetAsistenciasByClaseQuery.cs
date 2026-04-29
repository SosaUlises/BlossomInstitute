using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Asistencia.Queries.GetAsistenciasByClase
{
    public class GetAsistenciasByClaseQuery : IGetAsistenciasByClaseQuery
    {
        private readonly IDataBaseService _db;

        public GetAsistenciasByClaseQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int cursoId, DateOnly fecha, CancellationToken ct = default)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "CursoId inválido");

            var curso = await _db.Cursos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cursoId, ct);
            if (curso == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Curso no encontrado");

            var clase = await _db.Clases.AsNoTracking()
                .FirstOrDefaultAsync(x => x.CursoId == cursoId && x.Fecha == fecha, ct);

            if (clase == null)
            {
                // No hay clase creada => nadie tomó asistencia ese día
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "No existe clase registrada para esa fecha");
            }

            // Traer alumnos matriculados
            var alumnos = await _db.Matriculas.AsNoTracking()
                .Where(m => m.CursoId == cursoId)
                .Select(m => new
                {
                    m.AlumnoId,
                    Nombre = m.Alumno.Usuario.Nombre,
                    Apellido = m.Alumno.Usuario.Apellido
                })
                .ToListAsync(ct);

            var asistencias = await _db.Asistencias.AsNoTracking()
                .Where(a => a.ClaseId == clase.Id)
                .ToListAsync(ct);

            var asisByAlumnoId = asistencias.ToDictionary(x => x.AlumnoId, x => x);

            var items = alumnos
                .OrderBy(x => x.Apellido).ThenBy(x => x.Nombre)
                .Select(a =>
                {
                    asisByAlumnoId.TryGetValue(a.AlumnoId, out var reg);
                    return new AsistenciaAlumnoModel
                    {
                        AlumnoId = a.AlumnoId,
                        NombreCompleto = $"{a.Apellido} {a.Nombre}",
                        Estado = reg?.Estado
                    };
                })
                .ToList();

            var result = new ClaseAsistenciasModel
            {
                ClaseId = clase.Id,
                CursoId = cursoId,
                Fecha = fecha.ToString("yyyy-MM-dd"),
                EstadoClase = clase.Estado,
                Descripcion = clase.Descripcion,
                Alumnos = items
            };

            return ResponseApiService.Response(StatusCodes.Status200OK, result);
        }
    }
}
