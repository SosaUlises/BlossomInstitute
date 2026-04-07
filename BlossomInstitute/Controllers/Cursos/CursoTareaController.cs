using BlossomInstitute.Application.DataBase.Tarea.Commands.ArchivarTarea;
using BlossomInstitute.Application.DataBase.Tarea.Commands.CreateTarea;
using BlossomInstitute.Application.DataBase.Tarea.Commands.UpdateTarea;
using BlossomInstitute.Application.DataBase.Tarea.Queries.GetTareasByCurso;
using BlossomInstitute.Application.DataBase.Tarea.Queries.GetTareasById;
using BlossomInstitute.Common.Features;
using BlossomInstitute.Domain.Entidades.Tarea;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlossomInstitute.Controllers.Cursos
{
    [Route("api/v1/cursos/{cursoId:int}/tareas")]
    [ApiController]
    [Authorize(Roles = "Profesor,Administrador")]
    public class CursoTareaController : ControllerBase
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

        [HttpPut("{tareaId:int}")]
        public async Task<IActionResult> Update(
            [FromRoute] int cursoId,
            [FromRoute] int tareaId,
            [FromBody] UpdateTareaModel model,
            [FromServices] IUpdateTareaCommand command,
            [FromServices] IValidator<UpdateTareaModel> validator,
            CancellationToken ct)
        {
            var vr = await validator.ValidateAsync(model, ct);
            if (!vr.IsValid) return BadRequest(ResponseApiService.Response(400, vr.Errors));

            var result = await command.Execute(cursoId, tareaId, GetUserId(), model, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("{tareaId:int}/archivar")]
        public async Task<IActionResult> Archivar(
            [FromRoute] int cursoId,
            [FromRoute] int tareaId,
            [FromServices] IArchivarTareaCommand command,
            CancellationToken ct)
        {
            var result = await command.Execute(cursoId, tareaId, GetUserId(), ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
             [FromServices] IGetTareasByCursoQuery query,
             [FromRoute] int cursoId,
             [FromQuery] EstadoTarea? estado,
             [FromQuery] int pageNumber = 1,
             [FromQuery] int pageSize = 20,
             [FromQuery] string? search = null,
             CancellationToken ct = default)
        {
            var result = await query.Execute(cursoId, GetUserId(), pageNumber, pageSize, search, estado, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{tareaId:int}")]
        public async Task<IActionResult> GetById(
            [FromRoute] int cursoId,
            [FromRoute] int tareaId,
            [FromServices] IGetTareaByIdQuery query,
            CancellationToken ct = default)
        {
            var result = await query.Execute(cursoId, tareaId, GetUserId(), ct);
            return StatusCode(result.StatusCode, result);
        }

    }

}
