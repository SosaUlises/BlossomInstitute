using BlossomInstitute.Application.DataBase.Asistencia.Queries.GetMisAsistencias;
using BlossomInstitute.Application.DataBase.Curso.Queries.GetMyCursos.Alumno;
using BlossomInstitute.Common.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlossomInstitute.Controllers.Me.Alumnos
{
    [ApiController]
    [Authorize(Roles = "Alumno")]
    [Route("api/v1/me")]
    public class MeAlumnoCursosController : ControllerBase
    {

        [HttpGet("alumno/cursos")]
        public async Task<IActionResult> GetMyCursosAlumno(
       [FromServices] IGetMyCursosAlumnoQuery query,
       [FromQuery] int pageNumber = 1,
       [FromQuery] int pageSize = 10,
       [FromQuery] string? search = null,
       [FromQuery] int? anio = null,
       [FromQuery] int? estado = null)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId) || userId <= 0)
                return Unauthorized(ResponseApiService.Response(401, message: "Token inválido"));

            var result = await query.Execute(userId, pageNumber, pageSize, search, anio, estado);
            return StatusCode(result.StatusCode, result);
        }

    }
}
