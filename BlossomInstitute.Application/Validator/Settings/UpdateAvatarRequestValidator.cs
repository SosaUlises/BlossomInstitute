using BlossomInstitute.Application.DataBase.Settings.Command.UpdateAvatar;
using FluentValidation;

namespace BlossomInstitute.Application.Validator.Settings
{
    public class UpdateAvatarRequestValidator : AbstractValidator<UpdateAvatarRequest>
    {
        private const long MaxAvatarSizeBytes = 5 * 1024 * 1024;

        private static readonly string[] AllowedContentTypes =
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        public UpdateAvatarRequestValidator()
        {
            RuleFor(x => x.File)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("Debe adjuntar una foto de perfil")
                .Must(file => file.Length > 0).WithMessage("Debe adjuntar una foto de perfil")
                .Must(file => file.Length <= MaxAvatarSizeBytes)
                    .WithMessage("La foto de perfil no puede superar los 5 MB")
                .Must(file => AllowedContentTypes.Contains(file.ContentType))
                    .WithMessage("Tipo de archivo no permitido. Use image/jpeg, image/png o image/webp");
        }
    }
}
