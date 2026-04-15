using BlossomInstitute.Application.DataBase.PlantillaCalificacion.Command.Archive;
using BlossomInstitute.Application.DataBase.PlantillaCalificacion.Command.CreatePlantilla;
using BlossomInstitute.Application.DataBase.PlantillaCalificacion.Command.Update;
using BlossomInstitute.Common.Features;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlossomInstitute.Controllers.Cursos
{
    [Route("api/v1/cursos/{cursoId:int}/plantillas-calificaciones")]
    [Authorize(Roles = "Profesor")]
    [ApiController]
    public class CursoPlantillaCalificacionesController : ControllerBase
    {
        private int GetUserId()
        {
            var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(v, out var id) ? id : 0;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromRoute] int cursoId,
            [FromBody] CreatePlantillaCalificacionModel model,
            [FromServices] ICreatePlantillaCalificacionCommand command,
            [FromServices] IValidator<CreatePlantillaCalificacionModel> validator,
            CancellationToken ct)
        {
            if (cursoId <= 0)
            {
                return BadRequest(ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    "Curso inválido"));
            }

            var validationResult = await validator.ValidateAsync(model, ct);

            if (!validationResult.IsValid)
            {
                return BadRequest(ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    validationResult.Errors));
            }

  
            var result = await command.Execute(cursoId, GetUserId(), model, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{plantillaId:int}")]
        public async Task<IActionResult> Update(
            [FromRoute] int cursoId,
            [FromRoute] int plantillaId,
            [FromBody] UpdatePlantillaCalificacionModel model,
            [FromServices] IUpdatePlantillaCalificacionCommand command,
            [FromServices] IValidator<UpdatePlantillaCalificacionModel> validator,
            CancellationToken ct)
        {
            if (cursoId <= 0 || plantillaId <= 0)
            {
                return BadRequest(ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    "Parámetros inválidos"));
            }

            var validationResult = await validator.ValidateAsync(model, ct);
            if (!validationResult.IsValid)
            {
                return BadRequest(ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    validationResult.Errors));
            }

      
            var result = await command.Execute(cursoId, plantillaId, GetUserId(), model, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("{plantillaId:int}/archive")]
        public async Task<IActionResult> Archive(
           [FromRoute] int cursoId,
           [FromRoute] int plantillaId,
           [FromServices] IArchivePlantillaCalificacionCommand command,
           CancellationToken ct)
        {
            if (cursoId <= 0 || plantillaId <= 0)
            {
                return BadRequest(ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    "Parámetros inválidos"));
            }

            var result = await command.Execute(cursoId, plantillaId, GetUserId(), ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
