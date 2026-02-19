using BlossomInstitute.Application.DataBase.Profesor.Command.CreateProfesor;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.Validator.Profesor
{
    public class CreateProfesorValidator : AbstractValidator<CreateProfesorModel>
    {
        public CreateProfesorValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El email es obligatorio.")
                .EmailAddress().WithMessage("El formato del email no es válido.")
                .MaximumLength(100);

            RuleFor(x => x.Nombre)
                .NotEmpty().WithMessage("El nombre es obligatorio.")
                .MaximumLength(30).WithMessage("El nombre no puede superar los 30 caracteres.")
                .Must(x => x.Trim().Length > 0).WithMessage("El nombre no puede contener solo espacios.");

            RuleFor(x => x.Apellido)
                .NotEmpty().WithMessage("El apellido es obligatorio.")
                .MaximumLength(30).WithMessage("El apellido no puede superar los 30 caracteres.")
                .Must(x => x.Trim().Length > 0).WithMessage("El apellido no puede contener solo espacios.");

            RuleFor(x => x.Dni)
                .GreaterThan(1000000).WithMessage("El DNI no es válido.")
                .LessThan(99999999).WithMessage("El DNI no es válido.");

            RuleFor(x => x.Telefono)
                .NotEmpty().WithMessage("El teléfono es obligatorio.")
                .MinimumLength(6).WithMessage("El teléfono no es válido.")
                .MaximumLength(20).WithMessage("El teléfono no es válido.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contraseña es obligatoria.")
                .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.")
                .MaximumLength(100);

        }
    }
}
