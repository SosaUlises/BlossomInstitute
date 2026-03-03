using BlossomInstitute.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.Entregas.Commands.UpsertEntregaAlumno
{
    public interface IUpsertEntregaAlumnoCommand
    {
        Task<BaseResponseModel> Execute(int tareaId, int userId, UpsertEntregaAlumnoModel model, CancellationToken ct);
    }
}
