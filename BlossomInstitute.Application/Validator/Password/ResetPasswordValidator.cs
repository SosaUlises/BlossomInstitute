using BlossomInstitute.Application.DataBase.Password.Command.ResetPassword;
using FluentValidation;

namespace BlossomInstitute.Application.Validator.Password
{
    public class ResetPasswordValidator : AbstractValidator<ResetPasswordModel>
    {
        public ResetPasswordValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El email es obligatorio.")
                .EmailAddress().WithMessage("El formato del email no es válido.");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("El token es obligatorio.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("La nueva contraseña es obligatoria.")
                .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirmar contraseña es obligatorio.")
                .Equal(x => x.NewPassword).WithMessage("Las contraseñas no coinciden.");
        }
    }
}
