using BlossomInstitute.Application.DataBase.Entregas.Commands.CreateFeedbackEntrega;
using BlossomInstitute.Application.DataBase.Entregas.Commands.UpsertEntregaAlumno;
using BlossomInstitute.Common.Features;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlossomInstitute.Controllers
{
    [Route("api/v1")]
    [ApiController]
    public class EntregasController : ControllerBase
    {
        private int GetUserId()
        {
            var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(v, out var id) ? id : 0;
        }

        [Authorize(Roles = "Alumno")]
        [HttpPut("tareas/{tareaId:int}/mi-entrega")]
        public async Task<IActionResult> Upsert(
            [FromRoute] int tareaId,
            [FromBody] UpsertEntregaAlumnoModel model,
            [FromServices] IUpsertEntregaAlumnoCommand command,
            [FromServices] IValidator<UpsertEntregaAlumnoModel> validator,
            CancellationToken ct)
        {
            if (tareaId <= 0)
                return BadRequest(ResponseApiService.Response(400, "TareaId inválido"));

            var vr = await validator.ValidateAsync(model, ct);
            if (!vr.IsValid)
                return BadRequest(ResponseApiService.Response(400, vr.Errors));

            var userId = GetUserId();
            if (userId <= 0)
                return Unauthorized(ResponseApiService.Response(401, "No autenticado"));

            var result = await command.Execute(tareaId, userId, model, ct);
            return StatusCode(result.StatusCode, result);
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
                return BadRequest(ResponseApiService.Response(400, "Parámetros inválidos"));

            var vr = await validator.ValidateAsync(model, ct);
            if (!vr.IsValid)
                return BadRequest(ResponseApiService.Response(400, vr.Errors));

            var profesorUserId = GetUserId();
            if (profesorUserId <= 0)
                return Unauthorized(ResponseApiService.Response(401, "No autenticado"));

            var result = await command.Execute(cursoId, tareaId, alumnoId, profesorUserId, model, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
