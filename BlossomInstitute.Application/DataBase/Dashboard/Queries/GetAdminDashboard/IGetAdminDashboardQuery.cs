using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Dashboard.Queries.GetAdminDashboard
{
    public interface IGetAdminDashboardQuery
    {
        Task<BaseResponseModel> Execute(
            int userId,
            bool isAdmin,
            CancellationToken ct);
    }
}
