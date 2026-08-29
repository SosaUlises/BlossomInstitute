using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Curso.Queries.GetMyCursos.Alumno
{
    public class GetMyCursosAlumnoQuery : IGetMyCursosAlumnoQuery
    {
        private readonly IDataBaseService _db;

        public GetMyCursosAlumnoQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int userId, int pageNumber, int pageSize, string? search, int? anio, int? estado)
        {
            if (userId <= 0)
                return ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "Usuario inválido");

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            // Confirmar que el usuario sea alumno (PK compartida)
            var existeAlumno = await _db.Alumnos.AsNoTracking().AnyAsync(a => a.Id == userId);
            if (!existeAlumno)
                return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No sos Alumno");

            var q = _db.Cursos
                .AsNoTracking()
                .Where(c => c.Matriculas.Any(m => m.AlumnoId == userId))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLowerInvariant();
                q = q.Where(c => c.Nombre.ToLower().Contains(s));
            }

            if (anio.HasValue)
                q = q.Where(c => c.Anio == anio.Value);

            if (estado.HasValue)
            {
                if (estado.Value < 1 || estado.Value > 3)
                    return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Estado inválido");

                q = q.Where(c => (int)c.Estado == estado.Value);
            }

            var hoy = DateOnly.FromDateTime(DateTime.Now);
            var ahoraLocal = DateTime.Now;
            var total = await q.CountAsync();

            var cursos = await q
                .OrderByDescending(c => c.Anio)
                .ThenBy(c => c.Nombre)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Id,
                    c.Nombre,
                    c.Anio,
                    c.Descripcion,
                    c.ThemeIcon,
                    c.Estado,
                    CantidadHorarios = c.Horarios.Count,
                    CantidadAlumnos = c.Matriculas.Count,
                    Horarios = c.Horarios
                        .Select(h => new
                        {
                            h.Dia,
                            h.HoraInicio,
                            h.HoraFin
                        })
                        .ToList()
                })
                .ToListAsync();

            var items = cursos
                .Select(c =>
                {
                    var proximaClase = c.Horarios
                        .Select(h => new
                        {
                            h.Dia,
                            h.HoraInicio,
                            h.HoraFin,
                            Fecha = ObtenerProximaFecha(h.Dia, hoy, h.HoraInicio, ahoraLocal)
                        })
                        .OrderBy(x => x.Fecha)
                        .ThenBy(x => x.HoraInicio)
                        .FirstOrDefault();

                    return new CursoResumenModel
                    {
                        Id = c.Id,
                        Nombre = c.Nombre,
                        Anio = c.Anio,
                        Descripcion = c.Descripcion,
                        ThemeIcon = c.ThemeIcon,
                        Estado = c.Estado,
                        CantidadHorarios = c.CantidadHorarios,
                        CantidadAlumnos = c.CantidadAlumnos,
                        CantidadCompaneros = Math.Max(0, c.CantidadAlumnos - 1),
                        ProximaClaseFecha = proximaClase?.Fecha,
                        ProximaClaseDia = proximaClase?.Dia,
                        ProximaClaseHoraInicio = proximaClase?.HoraInicio.ToString("HH:mm"),
                        ProximaClaseHoraFin = proximaClase?.HoraFin.ToString("HH:mm")
                    };
                })
                .ToList();

            return ResponseApiService.Response(StatusCodes.Status200OK, new
            {
                pageNumber,
                pageSize,
                total,
                items
            });
        }

        private static DateOnly ObtenerProximaFecha(
            DayOfWeek diaClase,
            DateOnly hoy,
            TimeOnly horaInicio,
            DateTime ahoraLocal)
        {
            var diasHasta = ((int)diaClase - (int)hoy.DayOfWeek + 7) % 7;
            var fecha = hoy.AddDays(diasHasta);

            if (diasHasta == 0 && horaInicio <= TimeOnly.FromDateTime(ahoraLocal))
                fecha = fecha.AddDays(7);

            return fecha;
        }
    }
}
