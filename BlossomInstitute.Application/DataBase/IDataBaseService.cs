using BlossomInstitute.Domain.Entidades.Usuario;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlossomInstitute.Application.DataBase
{
    public interface IDataBaseService
    {
        Task<bool> SaveAsync(CancellationToken ct = default);
    }
}

