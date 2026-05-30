using BlossomInstitute.Application.DataBase.Alumno.Queries.GetAcademicSummary;
using BlossomInstitute.Common.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlossomInstitute.Controllers.Admin
{
    [Route("api/v1/admin/students")]
    [Authorize(Roles = "Administrador")]
    [ApiController]
    public class AdminStudentsController : ControllerBase
    {
        [HttpGet("{studentId:int}/academic-summary")]
        public async Task<IActionResult> GetAcademicSummary(
            [FromRoute] int studentId,
            [FromServices] IGetAlumnoAcademicSummaryQuery query,
            CancellationToken ct)
        {
            if (studentId <= 0)
                return BadRequest(ResponseApiService.Response(400, message: "Alumno invalido"));

            var result = await query.Execute(studentId, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
