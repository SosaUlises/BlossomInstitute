using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Alumno.Commands.UpdateAlumno
{
    public interface IUpdateAlumnoCommand
    {
        Task<BaseResponseModel> Execute(int userId, UpdateAlumnoModel model);
    }
}
