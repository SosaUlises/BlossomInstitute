using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Alumno.Commands.DesactivarAlumno
{
    public interface IDesactivarAlumnoCommand
    {
        Task<BaseResponseModel> Execute(int userId);
    }
}
