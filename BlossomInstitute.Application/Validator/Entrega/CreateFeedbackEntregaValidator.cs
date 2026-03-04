using BlossomInstitute.Application.DataBase.Entregas.Commands.CreateFeedbackEntrega;
using BlossomInstitute.Domain.Entidades.Entrega;
using FluentValidation;

namespace BlossomInstitute.Application.Validator.Entrega
{
    public class CreateFeedbackEntregaValidator : AbstractValidator<CreateFeedbackEntregaModel>
    {
        public CreateFeedbackEntregaValidator()
        {
            RuleFor(x => x.Estado).IsInEnum();

            RuleFor(x => x.Comentario)
                .MaximumLength(8000);

            RuleFor(x => x.ArchivoCorregidoUrl)
                .MaximumLength(2000)
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .When(x => !string.IsNullOrWhiteSpace(x.ArchivoCorregidoUrl))
                .WithMessage("ArchivoCorregidoUrl inválida.");

            RuleFor(x => x.ArchivoCorregidoNombre)
                .MaximumLength(200);

            RuleFor(x => x.Nota)
                .InclusiveBetween(0m, 10m)
                .When(x => x.Nota.HasValue)
                .WithMessage("La nota debe estar entre 0 y 10.");

            // Regla típica: si está Aprobado, exigir nota
            RuleFor(x => x.Nota)
                .NotNull()
                .When(x => x.Estado == EstadoCorreccion.Aprobado)
                .WithMessage("La nota es obligatoria cuando el estado es Aprobado.");
        }
    }
}
