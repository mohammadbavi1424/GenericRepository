using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepositories.Contracts.GenericCleanArchitecture
{
    public interface IRepositoryTransaction
    {
        Task BeginTransactionAsync(CancellationToken cancellationToken);
        Task CommitTransactionAsync(CancellationToken cancellationToken);
        Task RollbackTransactionAsync(CancellationToken cancellationToken);
    }
}
