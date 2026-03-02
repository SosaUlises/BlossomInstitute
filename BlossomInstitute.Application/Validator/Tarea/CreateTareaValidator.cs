using BlossomInstitute.Application.DataBase.Tarea.Commands.CreateTarea;
using FluentValidation;

namespace BlossomInstitute.Application.Validator.Tarea
{
    public class CreateTareaValidator : AbstractValidator<CreateTareaModel>
    {
        public CreateTareaValidator()
        {
            RuleFor(x => x.Titulo)
                .NotEmpty().WithMessage("El título es obligatorio.")
                .MaximumLength(120);

            RuleFor(x => x.Consigna)
                .MaximumLength(4000);

            RuleForEach(x => x.Recursos).ChildRules(r =>
            {
                r.RuleFor(x => x.Url)
                    .NotEmpty().WithMessage("Url obligatoria.")
                    .MaximumLength(2000);

                r.RuleFor(x => x.Nombre)
                    .MaximumLength(200);
            });
        }
    }
}
