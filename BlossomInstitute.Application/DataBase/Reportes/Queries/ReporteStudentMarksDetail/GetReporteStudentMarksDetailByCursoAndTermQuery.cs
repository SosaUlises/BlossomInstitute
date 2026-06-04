using BlossomInstitute.Application.Common.Academic;
using BlossomInstitute.Application.DataBase.Reportes.Shared;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Calificacion;
using BlossomInstitute.Domain.Entidades.Calificaciones;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteStudentMarksDetail
{
    public class GetReporteStudentMarksDetailByCursoAndTermQuery : IGetReporteStudentMarksDetailByCursoAndTermQuery
    {
        private readonly IDataBaseService _db;

        public GetReporteStudentMarksDetailByCursoAndTermQuery(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(
            int cursoId,
            int alumnoId,
            int year,
            int term,
            int userId,
            bool isAdmin,
            int? tipo,
            CancellationToken ct)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "CursoId inválido");

            if (alumnoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "AlumnoId inválido");

            if (year < 2000 || year > 2100)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Year inválido");

            if (term < 1 || term > 3)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Term inválido. Valores permitidos: 1, 2 o 3.");

            if (tipo.HasValue && !Enum.IsDefined(typeof(TipoCalificacion), tipo.Value))
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Tipo inválido");

            var curso = await _db.Cursos
                .AsNoTracking()
                .Where(x => x.Id == cursoId)
                .Select(x => new
                {
                    x.Id,
                    x.Nombre
                })
                .FirstOrDefaultAsync(ct);

            if (curso == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Curso no encontrado");

            if (!isAdmin)
            {
                var profesorAsignado = await _db.CursoProfesores
                    .AsNoTracking()
                    .AnyAsync(x => x.CursoId == cursoId && x.ProfesorId == userId, ct);

                if (!profesorAsignado)
                    return ResponseApiService.Response(StatusCodes.Status403Forbidden, message: "No autorizado");
            }

            var alumno = await _db.Alumnos
                .AsNoTracking()
                .Where(x => x.Id == alumnoId)
                .Select(x => new
                {
                    x.Id,
                    Nombre = x.Usuario.Nombre,
                    Apellido = x.Usuario.Apellido,
                    Dni = x.Usuario.Dni,
                    Email = x.Usuario.Email
                })
                .FirstOrDefaultAsync(ct);

            if (alumno == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Alumno no encontrado");

            var alumnoMatriculado = await _db.Matriculas
                .AsNoTracking()
                .AnyAsync(x => x.CursoId == cursoId && x.AlumnoId == alumnoId, ct);

            if (!alumnoMatriculado)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "El alumno no pertenece al curso");

            var academicPeriod = AcademicQuarterHelper.GetQuarter(year, term);
            var from = academicPeriod.From;
            var to = academicPeriod.To;

            IQueryable<CalificacionEntity> q = _db.Calificaciones
                .AsNoTracking()
                .Where(x =>
                    x.CursoId == cursoId &&
                    x.AlumnoId == alumnoId &&
                    !x.Archivado &&
                    x.Fecha >= from &&
                    x.Fecha <= to &&
                    (
                        x.Tipo == TipoCalificacion.Homework ||
                        x.Tipo == TipoCalificacion.Quiz ||
                        x.Tipo == TipoCalificacion.Test ||
                        x.Tipo == TipoCalificacion.Participation ||
                        x.Tipo == TipoCalificacion.Behaviour
                    ));

            if (tipo.HasValue)
            {
                var tipoEnum = (TipoCalificacion)tipo.Value;
                q = q.Where(x => x.Tipo == tipoEnum);
            }

            var calificaciones = await q
                .OrderByDescending(x => x.Fecha)
                .ThenByDescending(x => x.Id)
                .Select(x => new ReporteStudentMarksDetailItemModel
                {
                    CalificacionId = x.Id,
                    Tipo = x.Tipo,
                    Titulo = x.Titulo,
                    Descripcion = x.Descripcion,
                    Nota = x.Nota,
                    Fecha = x.Fecha,
                    TareaId = x.TareaId,
                    EntregaId = x.EntregaId,
                    TieneDetalleSkills = x.TieneDetalleSkills,
                    Skills = x.Detalles
                        .OrderBy(d => d.Skill)
                        .Select(d => new ReporteStudentMarksDetailSkillModel
                        {
                            Skill = d.Skill,
                            PuntajeObtenido = d.PuntajeObtenido,
                            PuntajeMaximo = d.PuntajeMaximo,
                            Porcentaje = d.PuntajeMaximo > 0
                                ? Math.Round((d.PuntajeObtenido / d.PuntajeMaximo) * 100m, 2)
                                : null
                        })
                        .ToList()
                })
                .ToListAsync(ct);

            var response = new ReporteStudentMarksDetailResponseModel
            {
                CursoId = curso.Id,
                CursoNombre = curso.Nombre,
                AlumnoId = alumno.Id,
                AlumnoNombre = alumno.Nombre,
                AlumnoApellido = alumno.Apellido,
                AlumnoDni = alumno.Dni,
                AlumnoEmail = alumno.Email,
                Year = year,
                Term = term,
                From = from,
                To = to,
                Period = ReportAcademicPeriodModel.FromQuarter(academicPeriod),
                Total = calificaciones.Count,
                Items = calificaciones
            };

            return ResponseApiService.Response(StatusCodes.Status200OK, response);
        }
    }
}

