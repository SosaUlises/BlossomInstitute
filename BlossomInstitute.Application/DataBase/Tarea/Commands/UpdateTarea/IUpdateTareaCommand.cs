using BlossomInstitute.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.Tarea.Commands.UpdateTarea
{
    public interface IUpdateTareaCommand
    {
        Task<BaseResponseModel> Execute(int cursoId, int tareaId, int profesorUserId, UpdateTareaModel model, CancellationToken ct = default);
    }
}
