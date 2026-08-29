using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Clase;
using BlossomInstitute.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlossomInstitute.Application.DataBase.Clase.Commands
{
    public class CancelarClaseCommand : ICancelarClaseCommand
    {
        private readonly IDataBaseService _db;

        public CancelarClaseCommand(IDataBaseService db)
        {
            _db = db;
        }

        public async Task<BaseResponseModel> Execute(int cursoId, DateOnly fecha, CancellationToken ct = default)
        {
            if (cursoId <= 0)
                return ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "CursoId inválido");

            var clase = await _db.Clases
                .FirstOrDefaultAsync(x => x.CursoId == cursoId && x.Fecha == fecha, ct);

            if (clase == null)
            {
                clase = new ClaseEntity
                {
                    CursoId = cursoId,
                    Fecha = fecha,
                    Estado = EstadoClase.Cancelada
                };
                _db.Clases.Add(clase);
                await _db.SaveAsync(ct);

                return ResponseApiService.Response(StatusCodes.Status200OK, new { cursoId, fecha = fecha.ToString("yyyy-MM-dd") }, "Clase cancelada");
            }

            if (clase.Estado == EstadoClase.Cancelada)
                return ResponseApiService.Response(StatusCodes.Status200OK, message: "La clase ya estaba cancelada");

            await using var tx = await _db.BeginTransactionAsync(ct);

            clase.Estado = EstadoClase.Cancelada;

            // Borrar asistencias de esa clase para evitar ruido
            var asistencias = await _db.Asistencias.Where(a => a.ClaseId == clase.Id).ToListAsync(ct);
            if (asistencias.Count > 0)
                _db.Asistencias.RemoveRange(asistencias);

            await _db.SaveAsync(ct);
            await tx.CommitAsync(ct);

            return ResponseApiService.Response(StatusCodes.Status200OK, new { cursoId, fecha = fecha.ToString("yyyy-MM-dd") }, "Clase cancelada");
        }
    }
}
