using BlossomInstitute.Application.DataBase.Profesor.Command.CreateProfesor;
using BlossomInstitute.Common.Features;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlossomInstitute.Controllers
{
    [Route("api/v1/profesores")]
    [ApiController]
    public class ProfesorController : ControllerBase
    {
        [Authorize(Roles = "Administrador")]
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
    }
}
