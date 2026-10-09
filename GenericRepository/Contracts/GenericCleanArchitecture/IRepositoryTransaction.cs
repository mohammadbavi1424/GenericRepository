using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Contracts.GenericCleanArchitecture
{
    public interface IRepositoryTransaction
    {
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}
