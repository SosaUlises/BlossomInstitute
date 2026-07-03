using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Alumno.Commands.CreateAlumno
{
    public interface ICreateAlumnoCommand
    {
        Task<BaseResponseModel> Execute(CreateAlumnoModel model);
    }
}
