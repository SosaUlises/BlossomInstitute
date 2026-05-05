using BlossomInstitute.Application.DataBase.Curso.Queries.GetPersonasAlumnoCurso;
using BlossomInstitute.Common.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlossomInstitute.Controllers.Me.Alumnos
{
    [ApiController]
    [Authorize(Roles = "Alumno")]
    [Route("api/v1/me/alumno/cursos/{cursoId:int}/personas")]
    public class MeAlumnoCursoPersonasController : ControllerBase
    {
        private int GetUserId()
        {
            var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(v, out var id) ? id : 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetPersonas(
            [FromRoute] int cursoId,
            [FromServices] IGetPersonasAlumnoCursoQuery query,
            CancellationToken ct = default)
        {
            var userId = GetUserId();
            if (userId <= 0)
                return Unauthorized(ResponseApiService.Response(StatusCodes.Status401Unauthorized, message: "Token invÃ¡lido"));

            var result = await query.Execute(cursoId, userId, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
