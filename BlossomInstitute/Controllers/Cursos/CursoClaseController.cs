using BlossomInstitute.Application.DataBase.Clase.Commands;
using BlossomInstitute.Application.DataBase.Clase.Queries.GetClasesByCurso;
using BlossomInstitute.Common.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlossomInstitute.Controllers.Cursos
{
    [Route("api/v1/cursos/{cursoId:int}/clases")]
    [ApiController]
    [Authorize(Roles = "Administrador,Profesor")]
    public class CursoClaseController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetClasesByCurso(
            [FromServices] IGetClasesByCursoQuery query,
            [FromRoute] int cursoId,
            [FromQuery] string? from,
            [FromQuery] string? to,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default)
        {
            if (cursoId <= 0)
                return BadRequest(ResponseApiService.Response(400, message: "CursoId invalido"));

            DateOnly? fechaDesde = null;
            DateOnly? fechaHasta = null;

            if (!string.IsNullOrWhiteSpace(from))
            {
                if (!DateOnly.TryParse(from, out var d))
                    return BadRequest(ResponseApiService.Response(400, message: "from invalido. Formato esperado: yyyy-MM-dd"));

                fechaDesde = d;
            }

            if (!string.IsNullOrWhiteSpace(to))
            {
                if (!DateOnly.TryParse(to, out var d))
                    return BadRequest(ResponseApiService.Response(400, message: "to invalido. Formato esperado: yyyy-MM-dd"));

                fechaHasta = d;
            }

            if (fechaDesde.HasValue && fechaHasta.HasValue && fechaDesde > fechaHasta)
                return BadRequest(ResponseApiService.Response(400, message: "El rango de fechas es invalido (from > to)"));

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 200) pageSize = 200;

            var result = await query.Execute(cursoId, fechaDesde, fechaHasta, pageNumber, pageSize, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("{fecha}/cancelar")]
        public async Task<IActionResult> CancelarClase(
           [FromRoute] int cursoId,
           [FromRoute] string fecha,
           [FromServices] ICancelarClaseCommand command,
           CancellationToken ct)
        {
            if (cursoId <= 0)
                return BadRequest(ResponseApiService.Response(400, message: "CursoId invalido"));

            if (!DateOnly.TryParse(fecha, out var date))
                return BadRequest(ResponseApiService.Response(400, message: "Fecha invalida. Formato esperado: yyyy-MM-dd"));

            var result = await command.Execute(cursoId, date, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
