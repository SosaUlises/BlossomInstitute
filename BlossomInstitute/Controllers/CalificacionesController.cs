using BlossomInstitute.Application.DataBase.Calificacion.Commands.ArchiveCalificacion;
using BlossomInstitute.Application.DataBase.Calificacion.Commands.CreateCalificacion;
using BlossomInstitute.Application.DataBase.Calificacion.Commands.UpdateCalificacion;
using BlossomInstitute.Application.DataBase.Calificacion.Queries.GetCalificacionesByAlumno;
using BlossomInstitute.Common.Features;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlossomInstitute.Controllers
{
    [ApiController]
    [Route("api/v1/cursos/{cursoId:int}/alumnos/{alumnoId:int}/calificaciones")]
    [Authorize(Roles = "Profesor,Administrador")]
    public class CalificacionesController : ControllerBase
    {
        private int GetUserId()
        {
            var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(v, out var id) ? id : 0;
        }

        private bool IsAdmin() => User.IsInRole("Administrador");
        private bool IsProfesor() => User.IsInRole("Profesor");

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

        [HttpPatch("{calificacionId:int}/archivar")]
        public async Task<IActionResult> Archive(
            [FromRoute] int cursoId,
            [FromRoute] int alumnoId,
            [FromRoute] int calificacionId,
            [FromServices] IArchiveCalificacionCommand command,
            CancellationToken ct)
        {
            var result = await command.Execute(cursoId, alumnoId, calificacionId, GetUserId(), ct);
            return StatusCode(result.StatusCode, result);
        }


        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetByAlumno(
            [FromRoute] int cursoId,
            [FromRoute] int alumnoId,
            [FromServices] IGetCalificacionesByAlumnoQuery query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var result = await query.Execute(alumnoId, GetUserId(), IsAdmin(), IsProfesor(), cursoId, pageNumber, pageSize, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
