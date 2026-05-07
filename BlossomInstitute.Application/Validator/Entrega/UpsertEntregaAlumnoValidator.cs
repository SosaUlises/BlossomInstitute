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

            RuleFor(x => x)
                .Must(x =>
                    !string.IsNullOrWhiteSpace(x.Texto) ||
                    (x.Adjuntos != null && x.Adjuntos.Any(a =>
                        a.Tipo == Domain.Entidades.Entrega.TipoAdjunto.Link
                            ? !string.IsNullOrWhiteSpace(a.Url)
                            : !string.IsNullOrWhiteSpace(a.Url) || !string.IsNullOrWhiteSpace(a.StorageKey))))
                .WithMessage("La entrega debe tener texto o al menos un adjunto.");

            RuleForEach(x => x.Adjuntos)
                .SetValidator(new UpsertEntregaAdjuntoValidator());
        }
    }
}
