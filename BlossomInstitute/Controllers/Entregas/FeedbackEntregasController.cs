using BlossomInstitute.Application.DataBase.Entregas.Commands.CreateFeedbackEntrega;
using BlossomInstitute.Common.Features;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlossomInstitute.Controllers.Entregas
{
    [Route("api/v1")]
    [ApiController]
    [Authorize(Roles = "Profesor")]
    public class FeedbackEntregasController : ControllerBase
    {
        private int GetUserId()
        {
            var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(v, out var id) ? id : 0;
        }

        [Route("cursos/{cursoId:int}/tareas/{tareaId:int}/entregas/{alumnoId:int}/feedbacks")]
        [Authorize(Roles = "Profesor")]
        [HttpPost]
        public async Task<IActionResult> CreateFeedback(
            [FromRoute] int cursoId,
            [FromRoute] int tareaId,
            [FromRoute] int alumnoId,
            [FromBody] CreateFeedbackEntregaModel model,
            [FromServices] ICreateFeedbackEntregaCommand command,
            [FromServices] IValidator<CreateFeedbackEntregaModel> validator,
            CancellationToken ct)
        {
            if (cursoId <= 0 || tareaId <= 0 || alumnoId <= 0)
                return BadRequest(ResponseApiService.Response(400, message: "Parámetros inválidos"));

            var vr = await validator.ValidateAsync(model, ct);
            if (!vr.IsValid)
                return BadRequest(ResponseApiService.Response(400, vr.Errors));

            var profesorUserId = GetUserId();
            if (profesorUserId <= 0)
                return Unauthorized(ResponseApiService.Response(401, message: "No autenticado"));

            var result = await command.Execute(cursoId, tareaId, alumnoId, profesorUserId, model, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
