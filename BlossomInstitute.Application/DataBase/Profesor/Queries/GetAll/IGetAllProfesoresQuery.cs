using BlossomInstitute.Domain.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase.Profesor.Queries.GetAllProfesores
{
    public interface IGetAllProfesoresQuery
    {
        Task<BaseResponseModel> Execute(int pageNumber, int pageSize, string? search);
    }
}
