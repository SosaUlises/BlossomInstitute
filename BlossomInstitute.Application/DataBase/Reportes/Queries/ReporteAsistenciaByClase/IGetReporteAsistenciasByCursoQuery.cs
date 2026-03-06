using BlossomInstitute.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteAsistenciaByClase
{
    public interface IGetReporteAsistenciasByCursoQuery
    {
        Task<BaseResponseModel> Execute(
            int cursoId,
            int profesorUserId,
            bool isAdmin,
            DateOnly from,
            DateOnly to,
            int pageNumber,
            int pageSize,
            string? search,
            CancellationToken ct);
    }
}
