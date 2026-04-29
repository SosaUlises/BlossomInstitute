using BlossomInstitute.Application.DataBase.Calificacion.Commands.UpdateCalificacion;
using BlossomInstitute.Domain.Entidades.Calificacion;
using FluentValidation;

namespace BlossomInstitute.Application.Validator.Calificacion
{
    public class UpdateCalificacionValidator : AbstractValidator<UpdateCalificacionModel>
    {
        public UpdateCalificacionValidator()
        {
            RuleFor(x => x.Tipo)
                .IsInEnum();

            RuleFor(x => x.Titulo)
                .NotEmpty().WithMessage("El título es obligatorio.")
                .MaximumLength(100);

            RuleFor(x => x.Descripcion)
                .MaximumLength(500);

            RuleFor(x => x.Nota)
               .InclusiveBetween(0m, 100m)
               .When(x => x.Nota.HasValue)
               .WithMessage("La nota debe estar entre 0 y 100.");

            RuleFor(x => x.Nota)
                .Must(nota => nota is 100m or 90m or 80m or 65m)
                .When(x => x.Tipo == TipoCalificacion.Participation || x.Tipo == TipoCalificacion.Behaviour)
                .WithMessage("Participation y Behaviour solo admiten notas 100, 90, 80 o 65.");

            RuleFor(x => x.Fecha)
                .NotEmpty()
                .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("La fecha no puede ser futura.");
        }
    }
}
