using BlossomInstitute.Application.DataBase.Calificacion.Commands.UpdateCalificacion;
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
                .InclusiveBetween(1, 10)
                .WithMessage("La nota debe estar entre 1 y 10.");

            RuleFor(x => x.Fecha)
                .NotEmpty()
                .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("La fecha no puede ser futura.");
        }
    }
}
