using BlossomInstitute.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.PlantillaCalificacion.Commands.Apply
{
    public interface IApplyPlantillaCalificacionCommand
    {
        Task<BaseResponseModel> Execute(
            int cursoId,
            int plantillaId,
            int profesorUserId,
            ApplyPlantillaCalificacionModel model,
            CancellationToken ct);
    }
}
