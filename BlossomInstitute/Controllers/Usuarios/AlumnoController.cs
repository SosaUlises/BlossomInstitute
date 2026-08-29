using BlossomInstitute.Application.DataBase.Alumno.Commands.ActivarAlumno;
using BlossomInstitute.Application.DataBase.Alumno.Commands.CreateAlumno;
using BlossomInstitute.Application.DataBase.Alumno.Commands.DesactivarAlumno;
using BlossomInstitute.Application.DataBase.Alumno.Commands.UpdateAlumno;
using BlossomInstitute.Application.DataBase.Alumno.Queries.GetAll;
using BlossomInstitute.Application.DataBase.Alumno.Queries.GetAsignableByCurso;
using BlossomInstitute.Application.DataBase.Alumno.Queries.GetById;
using BlossomInstitute.Common.Features;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlossomInstitute.Controllers.Usuarios
{
    [Route("api/v1/alumnos")]
    [Authorize(Roles = "Administrador")]
    [ApiController]
    public class AlumnoController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Create(
             [FromBody] CreateAlumnoModel model,
             [FromServices] ICreateAlumnoCommand createAlumnoCommand,
             [FromServices] IValidator<CreateAlumnoModel> validator)
        {
            var validationResult = await validator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                return BadRequest(ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    validationResult.Errors));
            }

            var result = await createAlumnoCommand.Execute(model);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{userId:int}")]
        public async Task<IActionResult> Update(
            [FromRoute] int userId,
            [FromBody] UpdateAlumnoModel model,
            [FromServices] IUpdateAlumnoCommand command,
            [FromServices] IValidator<UpdateAlumnoModel> validator)
        {
            if (userId <= 0) return BadRequest(ResponseApiService.Response(400, message: "Id inválido"));
            var vr = await validator.ValidateAsync(model);
            if (!vr.IsValid) return BadRequest(ResponseApiService.Response(400, vr.Errors));

            var result = await command.Execute(userId, model);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("{userId:int}/desactivar")]
        public async Task<IActionResult> Deactivate(
         [FromRoute] int userId,
         [FromServices] IDesactivarAlumnoCommand command)
        {
            if (userId <= 0)
                return BadRequest(ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Id inválido"));

            var result = await command.Execute(userId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("{userId:int}/activar")]
        public async Task<IActionResult> Activate(
         [FromRoute] int userId,
         [FromServices] IActivarAlumnoCommand command)
        {
            if (userId <= 0)
                return BadRequest(ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Id inválido"));

            var result = await command.Execute(userId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
             [FromServices] IGetAllAlumnosQuery query,
             [FromQuery] int pageNumber = 1,
             [FromQuery] int pageSize = 10,
             [FromQuery] string? search = null,
             CancellationToken ct = default)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var result = await query.Execute(pageNumber, pageSize, search, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{userId:int}")]
        public async Task<IActionResult> GetById(
            [FromRoute] int userId,
            [FromServices] IGetAlumnoByIdQuery query,
            CancellationToken ct = default)
        {
            if (userId <= 0) return BadRequest(ResponseApiService.Response(400, message: "Id inválido"));
            var result = await query.Execute(userId, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("assignable")]
        public async Task<IActionResult> GetAssignableByCurso(
            [FromQuery] int cursoId,
            [FromServices] IGetAsignableAlumnosByCursoQuery query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            CancellationToken ct = default)
        {
            if (cursoId <= 0)
                return BadRequest(ResponseApiService.Response(StatusCodes.Status400BadRequest, message: "Curso inválido"));

            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var result = await query.Execute(cursoId, pageNumber, pageSize, search, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
