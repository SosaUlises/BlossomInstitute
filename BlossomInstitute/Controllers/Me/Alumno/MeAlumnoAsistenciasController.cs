using BlossomInstitute.Application.DataBase.Asistencia.Queries.GetMisAsistencias;
using BlossomInstitute.Common.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlossomInstitute.Controllers.Me.Alumnos
{
    [ApiController]
    [Authorize(Roles = "Alumno")]
    [Route("api/v1/me")]
    public class MeAlumnoAsistenciasController : ControllerBase
    {

        [HttpGet("alumno/asistencias")]
        public async Task<IActionResult> GetMisAsistencias(
              [FromServices] IGetMisAsistenciasQuery query,
              [FromQuery] int? cursoId = null,
              [FromQuery] string? from = null,
              [FromQuery] string? to = null,
              [FromQuery] int pageNumber = 1,
              [FromQuery] int pageSize = 50,
              CancellationToken ct = default)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId) || userId <= 0)
                return Unauthorized(ResponseApiService.Response(401, message: "Token invalido"));

            if (cursoId.HasValue && cursoId.Value <= 0)
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

            var result = await query.Execute(userId, cursoId, fechaDesde, fechaHasta, pageNumber, pageSize, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
