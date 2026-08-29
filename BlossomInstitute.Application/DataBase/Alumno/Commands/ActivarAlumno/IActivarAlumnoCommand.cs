using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Alumno.Commands.ActivarAlumno
{
    public interface IActivarAlumnoCommand
    {
        Task<BaseResponseModel> Execute(int userId);
    }
}
