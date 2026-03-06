using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Calificacion.Queries.GetCalificacionesByAlumno
{
    public interface IGetCalificacionesByAlumnoQuery
    {
        Task<BaseResponseModel> Execute(
            int alumnoId,
            int userId,
            bool isAdmin,
            bool isProfesor,
            int? cursoId,
            int pageNumber,
            int pageSize,
            CancellationToken ct);
    }
}
