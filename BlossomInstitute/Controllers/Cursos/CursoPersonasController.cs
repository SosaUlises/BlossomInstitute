using BlossomInstitute.Application.DataBase.Curso.Commands.AsignarAlumnos;
using BlossomInstitute.Application.DataBase.Curso.Commands.AsignarProfesores;
using BlossomInstitute.Application.DataBase.Curso.Commands.RemoveAlumno;
using BlossomInstitute.Application.DataBase.Curso.Commands.RemoveProfesores;
using BlossomInstitute.Application.DataBase.Curso.Queries.GetAlumnosByCurso;
using BlossomInstitute.Application.DataBase.Curso.Queries.GetProfesoresByCurso;
using BlossomInstitute.Common.Features;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace BlossomInstitute.Controllers.Cursos
{
    [ApiController]
    [Authorize(Roles = "Administrador,Profesor")]
    [Route("api/v1/cursos/{cursoId:int}")]
    public class CursoPersonasController : ControllerBase
    {
        private int GetUserId()
        {
            var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(v, out var id) ? id : 0;
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Administrador");
        }

        [HttpGet("alumnos")]
        public async Task<IActionResult> GetAlumnosByCurso(
            [FromRoute] int cursoId,
            [FromServices] IGetAlumnosByCursoQuery query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            CancellationToken ct = default)
        {
            var result = await query.Execute(
                cursoId: cursoId,
                userId: GetUserId(),
                isAdmin: IsAdmin(),
                pageNumber: pageNumber,
                pageSize: pageSize,
                search: search,
                ct: ct);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("profesores")]
        public async Task<IActionResult> GetProfesoresByCurso(
            [FromRoute] int cursoId,
            [FromServices] IGetProfesoresByCursoQuery query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            CancellationToken ct = default)
        {
            var result = await query.Execute(
                cursoId: cursoId,
                userId: GetUserId(),
                isAdmin: IsAdmin(),
                pageNumber: pageNumber,
                pageSize: pageSize,
                search: search,
                ct: ct);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("assign/profesores")]
        public async Task<IActionResult> AssignProfesores(
            [FromRoute] int cursoId,
            [FromBody] AssignProfesoresToCursoModel model,
            [FromServices] IAssignProfesoresToCursoCommand command,
            [FromServices] IValidator<AssignProfesoresToCursoModel> validator,
            CancellationToken ct)
        {
            var vr = await validator.ValidateAsync(model, ct);
            if (!vr.IsValid)
                return BadRequest(ResponseApiService.Response(400, vr.Errors));

            var result = await command.Execute(cursoId, model, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("remove/profesores/{profesorId:int}")]
        public async Task<IActionResult> RemoveProfesor(
            [FromRoute] int cursoId,
            [FromRoute] int profesorId,
            [FromServices] IRemoveProfesorFromCursoCommand command,
            CancellationToken ct)
        {
            var result = await command.Execute(cursoId, profesorId, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("assign/alumnos")]
        public async Task<IActionResult> MatricularAlumnos(
            [FromRoute] int cursoId,
            [FromBody] MatricularAlumnosModel model,
            [FromServices] IMatricularAlumnosCommand command,
            [FromServices] IValidator<MatricularAlumnosModel> validator,
            CancellationToken ct)
        {
            var vr = await validator.ValidateAsync(model, ct);
            if (!vr.IsValid)
                return BadRequest(ResponseApiService.Response(400, vr.Errors));

            var result = await command.Execute(cursoId, model, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("remove/alumnos/{alumnoId:int}")]
        public async Task<IActionResult> RemoveAlumno(
            [FromRoute] int cursoId,
            [FromRoute] int alumnoId,
            [FromServices] IRemoveAlumnoFromCursoCommand command,
            CancellationToken ct)
        {
            var result = await command.Execute(cursoId, alumnoId, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}