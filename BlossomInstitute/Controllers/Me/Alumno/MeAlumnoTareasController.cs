using BlossomInstitute.Application.DataBase.Tarea.Queries.GetTareasAlumno;
using BlossomInstitute.Common.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlossomInstitute.Controllers.Me.Alumnos
{
    [ApiController]
    [Authorize(Roles = "Alumno")]
    [Route("api/v1/me/alumno/cursos/{cursoId:int}/tareas")]
    public class MeAlumnoTareasController : ControllerBase
    {
        private int GetUserId()
        {
            var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(v, out var id) ? id : 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetTareasByCurso(
            [FromRoute] int cursoId,
            [FromServices] IGetTareasAlumnoByCursoQuery query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var userId = GetUserId();
            if (userId <= 0)
                return Unauthorized(ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "Token inválido"));

            var result = await query.Execute(cursoId, userId, pageNumber, pageSize, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{tareaId:int}")]
        public async Task<IActionResult> GetTareaById(
            [FromRoute] int cursoId,
            [FromRoute] int tareaId,
            [FromServices] IGetTareaAlumnoByIdQuery query,
            CancellationToken ct = default)
        {
            var userId = GetUserId();
            if (userId <= 0)
                return Unauthorized(ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "Token inválido"));

            var result = await query.Execute(cursoId, tareaId, userId, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
