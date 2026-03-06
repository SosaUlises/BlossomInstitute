using BlossomInstitute.Application.DataBase.Entregas.Queries.GetEntregasByTarea;
using BlossomInstitute.Application.DataBase.Entregas.Queries.GetEntregasDetail;
using BlossomInstitute.Application.DataBase.Entregas.Queries.GetFeedbacksByEntrega;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlossomInstitute.Controllers
{
    [ApiController]
    [Authorize(Roles = "Profesor")]
    [Route("api/v1/cursos/{cursoId:int}/tareas/{tareaId:int}/entregas")]
    public class ProfesorEntregasController : ControllerBase
    {
        private int GetUserId()
        {
            var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(v, out var id) ? id : 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetEntregasByTarea(
            [FromRoute] int cursoId,
            [FromRoute] int tareaId,
            [FromServices] IGetEntregasByTareaQuery query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? estadoEntrega = null,
            [FromQuery] int? estadoFeedback = null,
            [FromQuery] bool? pendienteCorreccion = null,
            [FromQuery] bool? soloConAdjuntos = null,
            CancellationToken ct = default)
        {
            var result = await query.Execute(
                cursoId, tareaId, GetUserId(),
                pageNumber, pageSize,
                search,
                estadoEntrega, estadoFeedback,
                pendienteCorreccion, soloConAdjuntos,
                ct);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{alumnoId:int}")]
        public async Task<IActionResult> GetDetail(
            [FromRoute] int cursoId,
            [FromRoute] int tareaId,
            [FromRoute] int alumnoId,
            [FromServices] IGetEntregaDetailQuery query,
            CancellationToken ct = default)
        {
            var result = await query.Execute(cursoId, tareaId, alumnoId, GetUserId(), ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{alumnoId:int}/feedbacks")]
        public async Task<IActionResult> GetFeedbacks(
            [FromRoute] int cursoId,
            [FromRoute] int tareaId,
            [FromRoute] int alumnoId,
            [FromServices] IGetFeedbacksByEntregaQuery query,
            CancellationToken ct = default)
        {
            var result = await query.Execute(cursoId, tareaId, alumnoId, GetUserId(), ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
