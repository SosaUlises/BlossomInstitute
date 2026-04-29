using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Curso;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Curso.Commands.CreateCurso
{
    public class CreateCursoCommand : ICreateCursoCommand
    {
        private readonly IDataBaseService _db;

        public CreateCursoCommand(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(CreateCursoModel model)
        {
            var nombre = model.Nombre?.Trim();

            if (string.IsNullOrWhiteSpace(nombre))
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Nombre inválido");

            // Evitar duplicado por Año+Nombre
            var existe = await _db.Cursos.AnyAsync(c => c.Anio == model.Anio && c.Nombre == nombre);
            if (existe)
                return ResponseApiService.Response(StatusCodes.Status409Conflict, message: "Ya existe un curso con ese nombre para ese año");

            await using var tx = await _db.BeginTransactionAsync();

            try
            {
                var curso = new CursoEntity
                {
                    Nombre = nombre,
                    Anio = model.Anio,
                    Descripcion = model.Descripcion?.Trim(),
                    Estado = (EstadoCurso)model.Estado
                };

                foreach (var h in model.Horarios)
                {
                    // Parseo defensivo
                    TimeOnly.TryParse(h.HoraInicio, out var ini);
                    TimeOnly.TryParse(h.HoraFin, out var fin);

                    curso.Horarios.Add(new CursoHorarioEntity
                    {
                        Dia = (DayOfWeek)h.Dia,
                        HoraInicio = ini,
                        HoraFin = fin
                    });
                }

                _db.Cursos.Add(curso);
                await _db.SaveAsync();

                await tx.CommitAsync();

                return ResponseApiService.Response(StatusCodes.Status201Created, new
                {
                    curso.Id,
                    curso.Nombre,
                    curso.Anio,
                    curso.Estado
                }, "Curso creado correctamente");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
