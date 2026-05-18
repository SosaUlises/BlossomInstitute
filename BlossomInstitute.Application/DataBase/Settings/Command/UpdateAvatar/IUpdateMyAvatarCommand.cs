using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Settings.Command.UpdateAvatar
{
    public interface IUpdateMyAvatarCommand
    {
        Task<BaseResponseModel> Execute(
            int userId,
            UpdateAvatarRequest model,
            CancellationToken ct = default);
    }
}
