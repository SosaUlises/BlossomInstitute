using BlossomInstitute.Application.DataBase.Asistencia.Commands.TomarAsistencia;
using FluentValidation;

namespace BlossomInstitute.Application.Validator.Asistencia
{
    public class TomarAsistenciaValidator : AbstractValidator<TomarAsistenciaModel>
    {
        public TomarAsistenciaValidator()
        {
            RuleFor(x => x.Asistencias)
                .NotNull().WithMessage("Asistencias es obligatorio.")
                .Must(x => x.Count > 0).WithMessage("Debe enviar al menos una asistencia.");

            RuleForEach(x => x.Asistencias).ChildRules(item =>
            {
                item.RuleFor(x => x.AlumnoId).GreaterThan(0);
            });

            RuleFor(x => x.DescripcionClase)
                .MaximumLength(1000);
        }
    }
}
