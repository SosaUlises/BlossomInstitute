using Microsoft.AspNetCore.Http;

namespace BlossomInstitute.Application.DataBase.Settings.Command.UpdateAvatar
{
    public class UpdateAvatarRequest
    {
        public IFormFile File { get; set; } = default!;
    }
}
