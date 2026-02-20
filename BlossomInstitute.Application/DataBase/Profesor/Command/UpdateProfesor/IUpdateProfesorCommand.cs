using BlossomInstitute.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.Profesor.Command.UpdateProfesor
{
    public interface IUpdateProfesorCommand
    {
        Task<BaseResponseModel> Execute(int userId, UpdateProfesorModel model);
    }
}
