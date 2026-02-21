using BlossomInstitute.Application.DataBase.Profesor.Command.CreateProfesor;
using BlossomInstitute.Application.DataBase.Profesor.Command.DeleteProfesor;
using BlossomInstitute.Application.DataBase.Profesor.Command.UpdateProfesor;
using BlossomInstitute.Application.DataBase.Profesor.Queries.GetAllProfesores;
using BlossomInstitute.Application.DataBase.Profesor.Queries.GetById;
using BlossomInstitute.Common.Features;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlossomInstitute.Controllers
{
    [Route("api/v1/profesores")]
    [Authorize(Roles = "Administrador")]
    [ApiController]
    public class ProfesorController : ControllerBase
    {

        [HttpPost]
        public async Task<IActionResult> Create(
             [FromBody] CreateProfesorModel model,
             [FromServices] ICreateProfesorCommand createProfesorCommand,
             [FromServices] IValidator<CreateProfesorModel> validator)
        {
            var validationResult = await validator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                return BadRequest(ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    validationResult.Errors));
            }

            var result = await createProfesorCommand.Execute(model);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{userId:int}")]
        public async Task<IActionResult> Update(
            [FromRoute] int userId,
            [FromBody] UpdateProfesorModel model,
            [FromServices] IUpdateProfesorCommand command,
            [FromServices] IValidator<UpdateProfesorModel> validator)
        {
            if (userId <= 0) return BadRequest(ResponseApiService.Response(400, "Id inválido"));
            var vr = await validator.ValidateAsync(model);
            if (!vr.IsValid) return BadRequest(ResponseApiService.Response(400, vr.Errors));

            var result = await command.Execute(userId, model);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("{userId:int}/desactivar")]
        public async Task<IActionResult> Deactivate(
            int userId,
            [FromServices] IDesactivarProfesorCommand command)
        {
            if (userId <= 0)
                return BadRequest(ResponseApiService.Response(StatusCodes.Status400BadRequest, "Id inválido"));

            var result = await command.Execute(userId);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
             [FromServices] IGetAllProfesoresQuery query,
             [FromQuery] int pageNumber = 1,
             [FromQuery] int pageSize = 10,
             [FromQuery] string? search = null)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var result = await query.Execute(pageNumber, pageSize, search);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{userId:int}")]
        public async Task<IActionResult> GetById(
            [FromRoute] int userId,
            [FromServices] IGetProfesorByIdQuery query)
        {
            if (userId <= 0) return BadRequest(ResponseApiService.Response(400, "Id inválido"));
            var result = await query.Execute(userId);
            return StatusCode(result.StatusCode, result);
        }
    }
}
