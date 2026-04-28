using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Curso.Commands.UpdateCurso
{
    public class UpdateCursoCommand : IUpdateCursoCommand
    {
        private readonly IDataBaseService _db;

        public UpdateCursoCommand(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int cursoId, UpdateCursoModel model)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Id inválido");

            var nombre = model.Nombre?.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Nombre inválido");

            var curso = await _db.Cursos
                .Include(c => c.Horarios)
                .FirstOrDefaultAsync(c => c.Id == cursoId);

            if (curso == null)
                return ResponseApiService.Response(StatusCodes.Status404NotFound, message: "Curso no encontrado");

            // Evitar duplicado Año+Nombre en otro curso
            var existe = await _db.Cursos.AnyAsync(c => c.Id != cursoId && c.Anio == model.Anio && c.Nombre == nombre);
            if (existe)
                return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "Ya existe otro curso con ese nombre para ese año");

            await using var tx = await _db.BeginTransactionAsync();

            try
            {
                curso.Nombre = nombre;
                curso.Anio = model.Anio;
                curso.Descripcion = model.Descripcion?.Trim();
                curso.Estado = (EstadoCurso)model.Estado;

                // Estrategia: reemplazar horarios
                // borrar actuales
                if (curso.Horarios != null && curso.Horarios.Count > 0)
                {
                    _db.CursoHorarios.RemoveRange(curso.Horarios);
                }

                // agregar nuevos
                curso.Horarios = new List<CursoHorarioEntity>();

                foreach (var h in model.Horarios)
                {
                    TimeOnly.TryParse(h.HoraInicio, out var ini);
                    TimeOnly.TryParse(h.HoraFin, out var fin);

                    curso.Horarios.Add(new CursoHorarioEntity
                    {
                        CursoId = curso.Id,
                        Dia = (DayOfWeek)h.Dia,
                        HoraInicio = ini,
                        HoraFin = fin
                    });
                }

                await _db.SaveAsync();

                await tx.CommitAsync();

                return ResponseApiService.Response(StatusCodes.Status200OK, new
                {
                    curso.Id,
                    curso.Nombre,
                    curso.Anio,
                    curso.Estado
                }, "Curso actualizado correctamente");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
