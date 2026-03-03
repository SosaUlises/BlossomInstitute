using BlossomInstitute.Application.DataBase.Entregas.Commands.UpsertEntregaAlumno;
using FluentValidation;

namespace BlossomInstitute.Application.Validator.Entrega
{
    public class UpsertEntregaAlumnoValidator : AbstractValidator<UpsertEntregaAlumnoModel>
    {
        public UpsertEntregaAlumnoValidator()
        {
            RuleFor(x => x.Texto)
                .MaximumLength(8000);

            RuleForEach(x => x.Adjuntos)
                .SetValidator(new UpsertEntregaAdjuntoValidator());

            RuleFor(x => x.Adjuntos)
                .Must(list => list == null || list.Count <= 10)
                .WithMessage("Máximo 10 adjuntos por entrega.");
        }
    }
}
