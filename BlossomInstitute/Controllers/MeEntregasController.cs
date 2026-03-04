using BlossomInstitute.Application.DataBase.Entregas.Queries.Alumno.GetMiEntregaByTarea;
using BlossomInstitute.Application.DataBase.Entregas.Queries.Alumno.GetMisEntregasByCurso;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlossomInstitute.Controllers
{
    [ApiController]
    [Authorize(Roles = "Alumno")]
    [Route("api/v1/me")]
    public class MeEntregasController : ControllerBase
    {
        private int GetUserId()
        {
            var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(v, out var id) ? id : 0;
        }

        [HttpGet("tareas/{tareaId:int}/entrega")]
        public async Task<IActionResult> GetMiEntregaByTarea(
            [FromRoute] int tareaId,
            [FromServices] IGetMiEntregaByTareaQuery query,
            CancellationToken ct)
        {
            var result = await query.Execute(tareaId, GetUserId(), ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("cursos/{cursoId:int}/entregas")]
        public async Task<IActionResult> GetMisEntregasByCurso(
            [FromServices] IGetMisEntregasByCursoQuery query,
            CancellationToken ct,
            [FromRoute] int cursoId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await query.Execute(cursoId, GetUserId(), pageNumber, pageSize, ct);
            return StatusCode(result.StatusCode, result);
        }

    }
}
