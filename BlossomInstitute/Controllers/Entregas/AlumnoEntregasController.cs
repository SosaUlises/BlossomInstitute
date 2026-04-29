using BlossomInstitute.Application.DataBase.Entregas.Commands.UpsertEntregaAlumno;
using BlossomInstitute.Common.Features;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlossomInstitute.Controllers.Entregas
{
    [Route("api/v1")]
    [ApiController]
    [Authorize(Roles = "Alumno")]
    public class AlumnoEntregasController : ControllerBase
    {
        private int GetUserId()
        {
            var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(v, out var id) ? id : 0;
        }

        [Authorize(Roles = "Alumno")]
        [HttpPut("tareas/{tareaId:int}/entrega")]
        public async Task<IActionResult> Upsert(
            [FromRoute] int tareaId,
            [FromBody] UpsertEntregaAlumnoModel model,
            [FromServices] IUpsertEntregaAlumnoCommand command,
            [FromServices] IValidator<UpsertEntregaAlumnoModel> validator,
            CancellationToken ct)
        {
            if (tareaId <= 0)
                return BadRequest(ResponseApiService.Response(400, message: "TareaId inválido"));

            var vr = await validator.ValidateAsync(model, ct);
            if (!vr.IsValid)
                return BadRequest(ResponseApiService.Response(400, vr.Errors));

            var userId = GetUserId();
            if (userId <= 0)
                return Unauthorized(ResponseApiService.Response(401, message: "No autenticado"));

            var result = await command.Execute(tareaId, userId, model, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
