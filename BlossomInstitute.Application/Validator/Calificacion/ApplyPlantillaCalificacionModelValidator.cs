using BlossomInstitute.Application.DataBase.PlantillaCalificacion.Commands.Apply;
using FluentValidation;

namespace BlossomInstitute.Application.Validator.Calificacion
{
    public class ApplyPlantillaCalificacionModelValidator : AbstractValidator<ApplyPlantillaCalificacionModel>
    {
        public ApplyPlantillaCalificacionModelValidator()
        {
            RuleFor(x => x.Fecha)
                .NotEmpty()
                .WithMessage("La fecha es obligatoria");

            RuleFor(x => x.Alumnos)
                .NotNull()
                .WithMessage("Debe informar alumnos")
                .Must(x => x != null && x.Count > 0)
                .WithMessage("Debe informar al menos un alumno");

            RuleForEach(x => x.Alumnos).ChildRules(alumno =>
            {
                alumno.RuleFor(x => x.AlumnoId)
                    .GreaterThan(0)
                    .WithMessage("Alumno inválido");

                alumno.RuleFor(x => x.Detalles)
                    .NotNull()
                    .WithMessage("Debe informar detalles")
                    .Must(x => x != null && x.Count > 0)
                    .WithMessage("Debe informar al menos un detalle");

                alumno.RuleForEach(x => x.Detalles).ChildRules(detalle =>
                {
                    detalle.RuleFor(x => x.Skill)
                        .IsInEnum()
                        .WithMessage("Skill inválida");

                    detalle.RuleFor(x => x.PuntajeObtenido)
                        .GreaterThanOrEqualTo(0)
                        .WithMessage("El puntaje obtenido no puede ser negativo");
                });
            });
        }
    }
}

