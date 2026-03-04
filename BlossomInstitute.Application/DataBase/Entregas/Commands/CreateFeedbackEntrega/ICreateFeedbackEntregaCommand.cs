using BlossomInstitute.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.Entregas.Commands.CreateFeedbackEntrega
{
    public interface ICreateFeedbackEntregaCommand
    {
        Task<BaseResponseModel> Execute(
            int cursoId,
            int tareaId,
            int alumnoId,
            int profesorUserId,
            CreateFeedbackEntregaModel model,
            CancellationToken ct);
    }
}
