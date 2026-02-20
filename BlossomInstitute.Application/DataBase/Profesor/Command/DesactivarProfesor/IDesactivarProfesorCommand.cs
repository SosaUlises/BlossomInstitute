using BlossomInstitute.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.Profesor.Command.DeleteProfesor
{
    public interface IDesactivarProfesorCommand
    {
        Task<BaseResponseModel> Execute(int userId);
    }
}
