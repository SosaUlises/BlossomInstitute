using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Entidades.Tarea;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Curso.Queries.GetMyCursos.Alumno
{
    public class GetMyCursoDetalleAlumnoQuery : IGetMyCursoDetalleAlumnoQuery
    {
        private readonly IDataBaseService _db;

        public GetMyCursoDetalleAlumnoQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int userId, int cursoId, CancellationToken ct = default)
        {
            if (userId <= 0)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "Usuario inválido");

            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "CursoId inválido");

            var matriculado = await _db.Matriculas
                .AsNoTracking()
                .AnyAsync(x => x.CursoId == cursoId && x.AlumnoId == userId, ct);

            if (!matriculado)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No estás matriculado en este curso");

            var curso = await _db.Cursos
                .AsNoTracking()
                .Where(c => c.Id == cursoId)
                .Select(c => new
                {
                    c.Id,
                    c.Nombre,
                    c.Anio,
                    c.Estado,
                    c.Descripcion,
                    Horarios = c.Horarios
                        .OrderBy(h => h.Dia)
                        .ThenBy(h => h.HoraInicio)
                        .Select(h => new
                        {
                            Dia = (int)h.Dia,
                            HoraInicio = h.HoraInicio.ToString("HH:mm"),
                            HoraFin = h.HoraFin.ToString("HH:mm")
                        })
                        .ToList(),
                    Profesores = c.Profesores
                        .OrderBy(p => p.Profesor.Usuario.Apellido)
                        .ThenBy(p => p.Profesor.Usuario.Nombre)
                        .Select(p => new
                        {
                            p.Profesor.Usuario.Nombre,
                            p.Profesor.Usuario.Apellido
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync(ct);

            if (curso == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Curso no encontrado");

            var hoy = DateOnly.FromDateTime(DateTime.Now);

            var cantidadClases = await _db.Clases
                .AsNoTracking()
                .CountAsync(x => x.CursoId == cursoId && x.Estado != EstadoClase.Cancelada, ct);

            var tareasPendientes = await _db.Tareas
                .AsNoTracking()
                .CountAsync(x =>
                    x.CursoId == cursoId &&
                    x.Estado == EstadoTarea.Publicada &&
                    x.FechaEntregaUtc.HasValue &&
                    !_db.Entregas.Any(e => e.TareaId == x.Id && e.AlumnoId == userId), ct);

            var promedioCurso = await _db.Calificaciones
                .AsNoTracking()
                .Where(x => x.CursoId == cursoId && x.AlumnoId == userId && !x.Archivado)
                .Select(x => (decimal?)x.Nota)
                .AverageAsync(ct);

            var clasesTomadas = await _db.Clases
                .AsNoTracking()
                .CountAsync(x =>
                    x.CursoId == cursoId &&
                    x.Estado != EstadoClase.Cancelada &&
                    x.Fecha <= hoy, ct);

            var presentes = await _db.Asistencias
                .AsNoTracking()
                .CountAsync(x =>
                    x.AlumnoId == userId &&
                    x.Clase.CursoId == cursoId &&
                    x.Clase.Estado != EstadoClase.Cancelada &&
                    x.Clase.Fecha <= hoy &&
                    x.Estado == EstadoAsistencia.Presente, ct);

            var porcentajeAsistencia = clasesTomadas > 0
                ? Math.Round((decimal)presentes * 100 / clasesTomadas, 2)
                : 0;

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                CursoId = curso.Id,
                curso.Nombre,
                curso.Anio,
                curso.Estado,
                curso.Descripcion,
                curso.Horarios,
                curso.Profesores,
                cantidadClases,
                tareasPendientes,
                promedioCurso = promedioCurso.HasValue ? Math.Round(promedioCurso.Value, 2) : (decimal?)null,
                porcentajeAsistencia
            });
        }
    }
}
