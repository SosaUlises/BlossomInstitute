using BlossomInstitute.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.Profesor.Commands.UpdateProfesor
{
    public interface IUpdateProfesorCommand
    {
        Task<BaseResponseModel> Execute(int userId, UpdateProfesorModel model);
    }
}
