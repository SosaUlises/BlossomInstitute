using BlossomInstitute.Application.DataBase.Tarea.Commands.UpdateTarea;
using FluentValidation;

namespace BlossomInstitute.Application.Validator.Tarea
{
    public class UpdateTareaValidator : AbstractValidator<UpdateTareaModel>
    {
        public UpdateTareaValidator()
        {
            RuleFor(x => x.Titulo)
                .NotEmpty().WithMessage("El título es obligatorio.")
                .MaximumLength(120);

            RuleFor(x => x.Consigna)
                .MaximumLength(4000);

            RuleForEach(x => x.Recursos).ChildRules(r =>
            {
                r.RuleFor(x => x.Url)
                    .NotEmpty()
                    .MaximumLength(2000);

                r.RuleFor(x => x.Nombre)
                    .MaximumLength(200);
            });
        }
    }
}
