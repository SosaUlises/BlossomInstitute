using BlossomInstitute.Application.DataBase.Dashboard.Queries.GetAdminDashboard;
using BlossomInstitute.Common.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlossomInstitute.Controllers.Me.Admin
{
    [ApiController]
    [Route("api/v1/dashboard")]
    [Authorize(Roles = "Administrador")]
    public class MeAdminDashboard : ControllerBase
    {
        [HttpGet("admin")]
        public async Task<IActionResult> GetAdminDashboard(
            [FromServices] IGetAdminDashboardQuery query,
            CancellationToken ct)
        {
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                User.FindFirst("nameid")?.Value ??
                User.FindFirst("sub")?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized(ResponseApiService.Response(StatusCodes.Status401Unauthorized, "Usuario inválido"));

            var isAdmin = User.IsInRole("Administrador");

            var result = await query.Execute(userId, isAdmin, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}


