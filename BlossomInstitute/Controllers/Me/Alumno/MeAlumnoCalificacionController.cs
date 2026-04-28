using BlossomInstitute.Application.DataBase.Calificacion.Queries.GetCalificacionesByAlumno;
using BlossomInstitute.Common.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlossomInstitute.Controllers.Me.Alumno
{
    [ApiController]
    [Authorize(Roles = "Alumno")]
    [Route("api/v1/me/alumno/calificaciones")]
    public class MeAlumnoCalificacionController : ControllerBase
    {
        private int GetUserId()
        {
            var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(v, out var id) ? id : 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetMisCalificaciones(
           [FromServices] IGetCalificacionesByAlumnoQuery query,
           [FromQuery] int? cursoId = null,
           [FromQuery] int pageNumber = 1,
           [FromQuery] int pageSize = 10,
           CancellationToken ct = default)
        {
            var userId = GetUserId();
            if (userId <= 0)
                return Unauthorized(ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "Token inválido"));

            var result = await query.Execute(
                alumnoId: userId,
                userId: userId,
                isAdmin: false,
                isProfesor: false,
                cursoId: cursoId,
                pageNumber: pageNumber,
                pageSize: pageSize,
                ct: ct);

            return StatusCode(result.StatusCode, result);
        }
    }
}
