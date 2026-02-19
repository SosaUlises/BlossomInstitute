using BlossomInstitute.Application.DataBase.Login.Command;
using BlossomInstitute.Application.DataBase.Password.Command.ForgotPassword;
using BlossomInstitute.Application.DataBase.Password.Command.ResetPassword;
using BlossomInstitute.Common.Features;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlossomInstitute.Controllers
{
    [Route("/api/v1/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginModel model,
            [FromServices] ILoginCommand loginCommand,
            [FromServices] IValidator<LoginModel> validator
        )
        {
            var validationResult = await validator.ValidateAsync(model);

            if (!validationResult.IsValid)
            {
                return BadRequest(ResponseApiService.Response(
                    StatusCodes.Status400BadRequest,
                    validationResult.Errors));
            }

            var resultado = await loginCommand.Execute(model);

            return StatusCode(resultado.StatusCode, resultado);
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(
           [FromBody] ForgotPasswordModel model,
           [FromServices] IForgotPasswordCommand command,
           [FromServices] IValidator<ForgotPasswordModel> validator)
        {
            var validationResult = await validator.ValidateAsync(model);
            if (!validationResult.IsValid)
                return BadRequest(ResponseApiService.Response(400, validationResult.Errors));

            var result = await command.Execute(model);
            return StatusCode(result.StatusCode, result);
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordModel model,
            [FromServices] IResetPasswordCommand command,
            [FromServices] IValidator<ResetPasswordModel> validator)
        {
            var validationResult = await validator.ValidateAsync(model);
            if (!validationResult.IsValid)
                return BadRequest(ResponseApiService.Response(400, validationResult.Errors));

            var result = await command.Execute(model);
            return StatusCode(result.StatusCode, result);
        }
    }
}
