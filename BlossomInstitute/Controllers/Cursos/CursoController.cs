using BlossomInstitute.Application.DataBase.Curso.Commands.ActivarCurso;
using BlossomInstitute.Application.DataBase.Curso.Commands.ArchivarCurso;
using BlossomInstitute.Application.DataBase.Curso.Commands.CreateCurso;
using BlossomInstitute.Application.DataBase.Curso.Commands.DesactivarCurso;
using BlossomInstitute.Application.DataBase.Curso.Commands.UpdateCurso;
using BlossomInstitute.Application.DataBase.Curso.Commands.UpdateCursoTheme;
using BlossomInstitute.Application.DataBase.Curso.Queries.GetAllCursos;
using BlossomInstitute.Application.DataBase.Curso.Queries.GetCursoById;
using BlossomInstitute.Common.Features;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlossomInstitute.Controllers.Cursos
{
    [Route("api/v1/cursos")]
    [Authorize]
    [ApiController]
    public class CursosController : ControllerBase
    {

        private int GetUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : 0;
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Administrador");
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Create(
            [FromBody] CreateCursoModel model,
            [FromServices] ICreateCursoCommand command,
            [FromServices] IValidator<CreateCursoModel> validator)
        {
            var vr = await validator.ValidateAsync(model);
            if (!vr.IsValid)
                return BadRequest(ResponseApiService.Response(400, vr.Errors));

            var result = await command.Execute(model);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{cursoId:int}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Update(
            [FromRoute] int cursoId,
            [FromBody] UpdateCursoModel model,
            [FromServices] IUpdateCursoCommand command,
            [FromServices] IValidator<UpdateCursoModel> validator)
        {
            if (cursoId <= 0)
                return BadRequest(ResponseApiService.Response(400, message: "Id inválido"));

            var vr = await validator.ValidateAsync(model);
            if (!vr.IsValid)
                return BadRequest(ResponseApiService.Response(400, vr.Errors));

            var result = await command.Execute(cursoId, model);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{cursoId:int}/desactivar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Desactivate(
            [FromRoute] int cursoId,
            [FromServices] IDesactivateCursoCommand command)
        {
            if (cursoId <= 0)
                return BadRequest(ResponseApiService.Response(400, message: "Id inválido"));

            var result = await command.Execute(cursoId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{cursoId:int}/activar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Activate(
            [FromRoute] int cursoId,
            [FromServices] IActivateCursoCommand command)
        {
            if (cursoId <= 0)
                return BadRequest(ResponseApiService.Response(400, message: "Id inválido"));

            var result = await command.Execute(cursoId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{cursoId:int}/archivar")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Archive(
            [FromRoute] int cursoId,
            [FromServices] IArchiveCursoCommand command)
        {
            if (cursoId <= 0)
                return BadRequest(ResponseApiService.Response(400, message: "Id inválido"));

            var result = await command.Execute(cursoId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("{cursoId:int}/theme")]
        [Authorize(Roles = "Administrador,Profesor")]
        public async Task<IActionResult> UpdateTheme(
            [FromRoute] int cursoId,
            [FromBody] UpdateCursoThemeModel model,
            [FromServices] IUpdateCursoThemeCommand command,
            CancellationToken ct)
        {
            if (cursoId <= 0)
                return BadRequest(ResponseApiService.Response(400, message: "Id inválido"));

            var userId = GetUserId();
            if (userId <= 0)
                return Unauthorized(ResponseApiService.Response(401, message: "Token inválido"));

            var result = await command.Execute(cursoId, userId, IsAdmin(), model, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetAll(
            [FromServices] IGetAllCursosQuery query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? anio = null,
            [FromQuery] int? estado = null)
        {
            var result = await query.Execute(pageNumber, pageSize, search, anio, estado);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{cursoId:int}")]
        [Authorize(Roles = "Administrador,Profesor")]
        public async Task<IActionResult> GetById(
            [FromRoute] int cursoId,
            [FromServices] IGetCursoByIdQuery query,
            CancellationToken ct)
        {
            if (cursoId <= 0)
                return BadRequest(ResponseApiService.Response(400, message: "Id inválido"));

            var userId = GetUserId();
            if (userId <= 0)
                return Unauthorized(ResponseApiService.Response(401, message: "Token inválido"));

            var result = await query.Execute(cursoId, userId, IsAdmin(), ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
