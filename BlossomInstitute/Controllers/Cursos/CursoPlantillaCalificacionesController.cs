using BlossomInstitute.Application.DataBase.PlantillaCalificacion.Command.Apply;
using BlossomInstitute.Application.DataBase.PlantillaCalificacion.Command.Archive;
using BlossomInstitute.Application.DataBase.PlantillaCalificacion.Command.CreatePlantilla;
using BlossomInstitute.Application.DataBase.PlantillaCalificacion.Command.Update;
using BlossomInstitute.Application.DataBase.PlantillaCalificacion.Query.GetAll;
using BlossomInstitute.Application.DataBase.PlantillaCalificacion.Query.GetById;
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
            CancellationToken ct)
        {
            if (cursoId <= 0)
            {
                return BadRequest(ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    "Curso inválido"));
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
            CancellationToken ct)
        {
            if (cursoId <= 0 || plantillaId <= 0)
            {
                return BadRequest(ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    "Parámetros inválidos"));
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


        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromRoute] int cursoId,
            [FromQuery] int pageNumber,
            [FromQuery] int pageSize,
            [FromQuery] string? search,
            [FromServices] IGetAllPlantillaCalificacionesByCursoQuery query,
            CancellationToken ct)
        {
            if (cursoId <= 0)
                return BadRequest(ResponseApiService.Response(StatusCodes.Status400BadRequest, "Curso inválido"));


            var result = await query.Execute(cursoId, GetUserId(), pageNumber, pageSize, search, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{plantillaId:int}")]
        public async Task<IActionResult> GetById(
            [FromRoute] int cursoId,
            [FromRoute] int plantillaId,
            [FromServices] IGetPlantillaCalificacionByIdQuery query,
            CancellationToken ct)
        {
            if (cursoId <= 0 || plantillaId <= 0)
                return BadRequest(ResponseApiService.Response(StatusCodes.Status400BadRequest, "Parámetros inválidos"));

            var result = await query.Execute(cursoId, plantillaId, GetUserId(), ct);
            return StatusCode(result.StatusCode, result);
        }



        [HttpPost("{plantillaId:int}/apply")]
        public async Task<IActionResult> Apply(
            [FromRoute] int cursoId,
            [FromRoute] int plantillaId,
            [FromBody] ApplyPlantillaCalificacionModel model,
            [FromServices] IApplyPlantillaCalificacionCommand command,
            [FromServices] IValidator<ApplyPlantillaCalificacionModel> validator,
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
    }
}
