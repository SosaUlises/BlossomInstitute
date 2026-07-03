using BlossomInstitute.Application.DataBase.Settings.Commands.ChangePassword;
using FluentValidation;

namespace BlossomInstitute.Application.Validator.Settings
{
    public class ChangeMyPasswordModelValidator : AbstractValidator<ChangeMyPasswordModel>
    {
        public ChangeMyPasswordModelValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("La contraseña actual es obligatoria");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("La nueva contraseña es obligatoria")
                .MinimumLength(6).WithMessage("La nueva contraseña debe tener al menos 6 caracteres");

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty().WithMessage("La confirmación de la nueva contraseña es obligatoria");

            RuleFor(x => x)
                .Must(x => x.NewPassword == x.ConfirmNewPassword)
                .WithMessage("La nueva contraseña y su confirmación no coinciden");

            RuleFor(x => x)
                .Must(x => x.CurrentPassword != x.NewPassword)
                .WithMessage("La nueva contraseña debe ser distinta de la actual");
        }
    }
}