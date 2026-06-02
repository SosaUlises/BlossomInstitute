using BlossomInstitute.Application.DataBase.Curso.Queries.GetAcademicProfile;
using BlossomInstitute.Common.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlossomInstitute.Controllers.Admin
{
    [Route("api/v1/admin/courses")]
    [Authorize(Roles = "Administrador")]
    [ApiController]
    public class AdminCoursesController : ControllerBase
    {
        [HttpGet("{courseId:int}/academic-profile")]
        public async Task<IActionResult> GetAcademicProfile(
            [FromRoute] int courseId,
            [FromServices] IGetCourseAcademicProfileQuery query,
            CancellationToken ct)
        {
            if (courseId <= 0)
                return BadRequest(ResponseApiService.Response(400, message: "Curso invalido"));

            var result = await query.Execute(courseId, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}
