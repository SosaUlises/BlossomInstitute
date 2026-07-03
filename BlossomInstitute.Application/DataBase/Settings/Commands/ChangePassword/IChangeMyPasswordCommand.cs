using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Settings.Commands.ChangePassword
{
    public interface IChangeMyPasswordCommand
    {
        Task<BaseResponseModel> Execute(
            int userId,
            ChangeMyPasswordModel model,
            CancellationToken ct = default);
    }
}
