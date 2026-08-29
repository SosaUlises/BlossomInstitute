using BlossomInstitute.Application.DataBase.Settings.Commands.UpdateAccount;
using FluentValidation;

namespace BlossomInstitute.Application.Validator.Settings
{
    public class UpdateMyAccountSettingsModelValidator : AbstractValidator<UpdateMyAccountSettingsModel>
    {
        public UpdateMyAccountSettingsModelValidator()
        {
            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre es obligatorio")
                .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres");

            RuleFor(x => x.Apellido)
                .NotEmpty().WithMessage("El apellido es obligatorio")
                .MaximumLength(100).WithMessage("El apellido no puede superar los 100 caracteres");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El email es obligatorio")
                .EmailAddress().WithMessage("El email no es válido")
                .MaximumLength(256).WithMessage("El email no puede superar los 256 caracteres");

            RuleFor(x => x.Telefono)
                .MaximumLength(30).WithMessage("El teléfono no puede superar los 30 caracteres")
                .When(x => !string.IsNullOrWhiteSpace(x.Telefono));

            RuleFor(x => x.Dni)
                .GreaterThan(0).WithMessage("El DNI debe ser mayor a 0");
        }
    }
}

