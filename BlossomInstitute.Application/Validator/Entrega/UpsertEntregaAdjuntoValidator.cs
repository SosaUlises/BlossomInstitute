using BlossomInstitute.Application.DataBase.Entregas.Commands.UpsertEntregaAlumno;
using FluentValidation;

namespace BlossomInstitute.Application.Validator.Entrega
{
    public class UpsertEntregaAdjuntoValidator : AbstractValidator<UpsertEntregaAdjuntoModel>
    {
        public UpsertEntregaAdjuntoValidator()
        {
            RuleFor(x => x.Tipo).IsInEnum();

            RuleFor(x => x.Url)
                .NotEmpty().WithMessage("Url es obligatoria.")
                .MaximumLength(2000);

            RuleFor(x => x.Nombre)
                .MaximumLength(200);

            RuleFor(x => x.Url)
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .When(x => !string.IsNullOrWhiteSpace(x.Url))
                .WithMessage("Url inválida.");
        }
    }
}
