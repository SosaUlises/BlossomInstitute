using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Settings.Command.UpdateAccount
{
    public interface IUpdateMyAccountSettingsCommand
    {
        Task<BaseResponseModel> Execute(
            int userId,
            UpdateMyAccountSettingsModel model,
            CancellationToken ct = default);
    }
}
