using BlossomInstitute.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.Entregas.Queries.Alumno.GetMiEntregaByTarea
{
    public interface IGetMiEntregaByTareaQuery
    {
        Task<BaseResponseModel> Execute(int tareaId, int alumnoUserId, CancellationToken ct);
    }
}
