using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Login.Commands
{
    public interface ILoginCommand
    {
        Task<BaseResponseModel> Execute(LoginModel model);
    }
}
