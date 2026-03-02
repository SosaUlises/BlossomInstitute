using BlossomInstitute.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.Tarea.Commands.CreateTarea
{
    public interface ICreateTareaCommand
    {
        Task<BaseResponseModel> Execute(int cursoId, int profesorUserId, CreateTareaModel model, CancellationToken ct = default);
    }
}
