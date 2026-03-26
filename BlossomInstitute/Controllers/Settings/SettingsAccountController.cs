using BlossomInstitute.Application.DataBase.Settings.Command.ChangePassword;
using BlossomInstitute.Application.DataBase.Settings.Command.UpdateAccount;
using BlossomInstitute.Application.DataBase.Settings.Queries.GetMyAccount;
using BlossomInstitute.Common.Features;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BlossomInstitute.Controllers.Settings
{
    [ApiController]
    [Route("api/v1/settings/account")]
    [Authorize]
    public class SettingsAccountController : ControllerBase
    {
        private int GetUserId()
        {
            var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(v, out var id) ? id : 0;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyAccount(
            [FromServices] IGetMyAccountSettingsQuery query,
            CancellationToken ct = default)
        {
            var result = await query.Execute(GetUserId(), ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateMyAccount(
            [FromBody] UpdateMyAccountSettingsModel model,
            [FromServices] IUpdateMyAccountSettingsCommand command,
            [FromServices] IValidator<UpdateMyAccountSettingsModel> validator,
            CancellationToken ct = default)
        {
            var vr = await validator.ValidateAsync(model, ct);
            if (!vr.IsValid)
            {
                return BadRequest(ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    vr.Errors));
            }

            var result = await command.Execute(GetUserId(), model, ct);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPatch("change-password")]
        public async Task<IActionResult> ChangeMyPassword(
            [FromBody] ChangeMyPasswordModel model,
            [FromServices] IChangeMyPasswordCommand command,
            [FromServices] IValidator<ChangeMyPasswordModel> validator,
            CancellationToken ct = default)
        {
            var vr = await validator.ValidateAsync(model, ct);
            if (!vr.IsValid)
            {
                return BadRequest(ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    vr.Errors));
            }

            var result = await command.Execute(GetUserId(), model, ct);
            return StatusCode(result.StatusCode, result);
        }
    }
}