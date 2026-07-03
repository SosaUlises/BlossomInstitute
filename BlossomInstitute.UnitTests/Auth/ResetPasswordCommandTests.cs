using BlossomInstitute.Application.DataBase.Password.Commands.ResetPassword;
using BlossomInstitute.Domain.Entidades.Usuario;
using BlossomInstitute.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace BlossomInstitute.UnitTests.Auth
{
    public class ResetPasswordCommandTests
    {
        [Fact]
        public async Task Execute_Returns400_WhenPasswordsDontMatch()
        {
            var um = IdentityMocks.MockUserManager();
            var cmd = new ResetPasswordCommand(um.Object);

            var res = await cmd.Execute(new ResetPasswordModel
            {
                Email = "a@a.com",
                Token = "t",
                NewPassword = "123456",
                ConfirmPassword = "999999"
            });

            res.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task Execute_Returns403_WhenInactive()
        {
            var user = new UsuarioEntity { Email = "a@a.com", Activo = false };

            var um = IdentityMocks.MockUserManager();
            um.Setup(x => x.FindByEmailAsync("a@a.com")).ReturnsAsync(user);

            var cmd = new ResetPasswordCommand(um.Object);

            var res = await cmd.Execute(new ResetPasswordModel
            {
                Email = "a@a.com",
                Token = "t",
                NewPassword = "123456",
                ConfirmPassword = "123456"
            });

            res.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task Execute_Returns200_WhenResetOk()
        {
            var user = new UsuarioEntity { Email = "a@a.com", Activo = true };

            var um = IdentityMocks.MockUserManager();
            um.Setup(x => x.FindByEmailAsync("a@a.com")).ReturnsAsync(user);
            um.Setup(x => x.ResetPasswordAsync(user, "t", "123456")).ReturnsAsync(IdentityResult.Success);

            var cmd = new ResetPasswordCommand(um.Object);

            var res = await cmd.Execute(new ResetPasswordModel
            {
                Email = "a@a.com",
                Token = "t",
                NewPassword = "123456",
                ConfirmPassword = "123456"
            });

            res.StatusCode.Should().Be(StatusCodes.Status200OK);
            res.Success.Should().BeTrue();
        }
    }
}
