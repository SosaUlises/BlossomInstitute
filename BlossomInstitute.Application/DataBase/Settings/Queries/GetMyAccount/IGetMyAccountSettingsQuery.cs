using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Settings.Queries.GetMyAccount
{
    public interface IGetMyAccountSettingsQuery
    {
        Task<BaseResponseModel> Execute(int userId, CancellationToken ct = default);
    }
}
