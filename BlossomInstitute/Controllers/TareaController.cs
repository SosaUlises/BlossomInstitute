using BlossomInstitute.Application.DataBase.Tarea.Commands.CreateTarea;
using BlossomInstitute.Common.Features;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlossomInstitute.Controllers
{
    [Route("api/v1/cursos/{cursoId:int}/tareas")]
    [ApiController]
    [Authorize(Roles = "Profesor,Administrador")]
    public class TareaController : ControllerBase
    {
        private int GetUserId()
        {
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(s, out var id) ? id : 0;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromRoute] int cursoId,
            [FromBody] CreateTareaModel model,
            [FromServices] ICreateTareaCommand command,
            [FromServices] IValidator<CreateTareaModel> validator,
            CancellationToken ct)
        {
            var vr = await validator.ValidateAsync(model, ct);
            if (!vr.IsValid) return BadRequest(ResponseApiService.Response(400, vr.Errors));

            var result = await command.Execute(cursoId, GetUserId(), model, ct);
            return StatusCode(result.StatusCode, result);
        }

    }

}
