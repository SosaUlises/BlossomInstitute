using BlossomInstitute.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.Password.Command.ResetPassword
{
    public interface IResetPasswordCommand
    {
        Task<BaseResponseModel> Execute(ResetPasswordModel model);
    }
}
