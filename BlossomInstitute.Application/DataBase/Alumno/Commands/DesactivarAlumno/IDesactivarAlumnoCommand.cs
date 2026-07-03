using BlossomInstitute.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.Alumno.Commands.DesactivarAlumno
{
    public interface IDesactivarAlumnoCommand
    {
        Task<BaseResponseModel> Execute(int userId);
    }
}
