using BlossomInstitute.Application.DataBase.Asistencia.Commands.TomarAsistencia;
using FluentValidation;

namespace BlossomInstitute.Application.Validator.Asistencia
{
    public class TomarAsistenciaValidator : AbstractValidator<TomarAsistenciaModel>
    {
        public TomarAsistenciaValidator()
        {
            RuleFor(x => x.Asistencias)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("Asistencias es obligatorio.")
                .NotEmpty().WithMessage("Debe enviar al menos una asistencia.")
                .Must(asistencias => asistencias.Select(x => x.AlumnoId).Distinct().Count() == asistencias.Count)
                .WithMessage("No puede repetir alumnos en la misma toma de asistencia.");

            RuleForEach(x => x.Asistencias).ChildRules(item =>
            {
                item.RuleFor(x => x.AlumnoId)
                    .GreaterThan(0)
                    .WithMessage("AlumnoId invalido.");

                item.RuleFor(x => x.Estado)
                    .IsInEnum()
                    .WithMessage("Estado de asistencia invalido.");
            });

            RuleFor(x => x.DescripcionClase)
                .MaximumLength(1000);
        }
    }
}
