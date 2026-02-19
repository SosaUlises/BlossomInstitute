using BlossomInstitute.Domain.Entidades.Profesor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BlossomInstitute.Application.DataBase
{
    public interface IDataBaseService
    {

        DbSet<ProfesorEntity> Profesores { get; set; }
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
        Task<bool> SaveAsync(CancellationToken ct = default);
    }
}

