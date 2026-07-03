using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Profesor.Commands.ActivarProfesor
{
    public interface IActivarProfesorCommand
    {
        Task<BaseResponseModel> Execute(int userId);
    }
}
