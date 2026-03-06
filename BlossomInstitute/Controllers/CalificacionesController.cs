using BlossomInstitute.Application.DataBase.Calificacion.Commands.CreateCalificacion;
using BlossomInstitute.Application.DataBase.Calificacion.Commands.UpdateCalificacion;
using BlossomInstitute.Common.Features;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlossomInstitute.Controllers
{
    [ApiController]
    [Route("api/v1/cursos/{cursoId:int}/alumnos/{alumnoId:int}/calificaciones")]
    [Authorize(Roles = "Profesor")]
    public class CalificacionesController : ControllerBase
    {
        private int GetUserId()
        {
            var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(v, out var id) ? id : 0;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromRoute] int cursoId,
            [FromRoute] int alumnoId,
            [FromBody] CreateCalificacionModel model,
            [FromServices] ICreateCalificacionCommand command,
            [FromServices] IValidator<CreateCalificacionModel> validator,
            CancellationToken ct)
        {
            var vr = await validator.ValidateAsync(model, ct);
            if (!vr.IsValid)
                return BadRequest(ResponseApiService.Response(400, vr.Errors));

            var result = await command.Execute(cursoId, alumnoId, GetUserId(), model, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{calificacionId:int}")]
        public async Task<IActionResult> Update(
            [FromRoute] int cursoId,
            [FromRoute] int alumnoId,
            [FromRoute] int calificacionId,
            [FromBody] UpdateCalificacionModel model,
            [FromServices] IUpdateCalificacionCommand command,
            [FromServices] IValidator<UpdateCalificacionModel> validator,
            CancellationToken ct)
        {
            var vr = await validator.ValidateAsync(model, ct);
            if (!vr.IsValid)
                return BadRequest(ResponseApiService.Response(400, vr.Errors));

            var result = await command.Execute(cursoId, alumnoId, calificacionId, GetUserId(), model, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
