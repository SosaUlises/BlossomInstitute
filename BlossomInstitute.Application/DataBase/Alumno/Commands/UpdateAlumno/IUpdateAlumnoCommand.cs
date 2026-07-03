using BlossomInstitute.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.Alumno.Commands.UpdateAlumno
{
    public interface IUpdateAlumnoCommand
    {
        Task<BaseResponseModel> Execute(int userId, UpdateAlumnoModel model);
    }
}
