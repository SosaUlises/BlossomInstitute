using BlossomInstitute.Application.DataBase.Entregas.Commands.UpsertEntregaAlumno;
using BlossomInstitute.Common.Features;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlossomInstitute.Controllers
{
    [Route("api/v1/tareas/{tareaId:int}/mi-entrega")]
    [ApiController]
    [Authorize(Roles = "Alumno")]
    public class EntregasAlumnoController : ControllerBase
    {
        private int GetUserId()
        {
            var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(v, out var id) ? id : 0;
        }

        [HttpPut]
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
    }
}
