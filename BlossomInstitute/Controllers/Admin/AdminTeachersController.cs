using BlossomInstitute.Application.DataBase.Profesor.Queries.GetAcademicSummary;
using BlossomInstitute.Common.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlossomInstitute.Controllers.Admin
{
    [Route("api/v1/admin/teachers")]
    [Authorize(Roles = "Administrador")]
    [ApiController]
    public class AdminTeachersController : ControllerBase
    {
        [HttpGet("{teacherId:int}/academic-summary")]
        public async Task<IActionResult> GetAcademicSummary(
            [FromRoute] int teacherId,
            [FromServices] IGetProfesorAcademicSummaryQuery query,
            CancellationToken ct)
        {
            if (teacherId <= 0)
                return BadRequest(ResponseApiService.Response(400, message: "Profesor inválido"));

            var result = await query.Execute(teacherId, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
