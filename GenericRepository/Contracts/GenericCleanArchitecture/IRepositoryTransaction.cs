using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Contracts.GenericCleanArchitecture
{
    public interface IRepositoryTransaction
    {
        Task BeginTransactionAsync(CancellationToken cancellationToken);
        Task CommitTransactionAsync(CancellationToken cancellationToken);
        Task RollbackTransactionAsync(CancellationToken cancellationToken);
    }
}
