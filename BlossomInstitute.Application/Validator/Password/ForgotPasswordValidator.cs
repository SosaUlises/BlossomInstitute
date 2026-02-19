using BlossomInstitute.Application.DataBase.Password.Command.ForgotPassword;
using FluentValidation;

namespace BlossomInstitute.Application.Validator.Password
{
    public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordModel>
    {
        public ForgotPasswordValidator()
        {
            RuleFor(x => x.Email)
                    .NotEmpty().WithMessage("El email es obligatorio.")
                    .EmailAddress().WithMessage("El formato del email no es válido.");

        }
    }
}
