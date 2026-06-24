using BlossomInstitute.Domain.Model;

namespace BlossomInstitute.Application.DataBase.Curso.Commands.UpdateCursoTheme
{
    public interface IUpdateCursoThemeCommand
    {
        Task<BaseResponseModel> Execute(
            int cursoId,
            int userId,
            bool isAdmin,
            UpdateCursoThemeModel model,
            CancellationToken ct = default);
    }
}
