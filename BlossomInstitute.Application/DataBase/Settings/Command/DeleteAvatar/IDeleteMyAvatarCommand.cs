using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Settings.Command.DeleteAvatar
{
    public interface IDeleteMyAvatarCommand
    {
        Task<BaseResponseModel> Execute(
            int userId,
            CancellationToken ct = default);
    }
}
