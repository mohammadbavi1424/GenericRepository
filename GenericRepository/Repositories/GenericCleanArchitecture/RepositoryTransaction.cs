using GenericRepositories.Context;
using GenericRepositories.Contracts.GenericCleanArchitecture;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepositories.Repositories.GenericCleanArchitecture
{
    public class RepositoryTransaction : IRepositoryTransaction
    {
        private readonly GenericCommandDbContext DbCommandContext;
        private IDbContextTransaction? _transaction;

        public RepositoryTransaction(GenericCommandDbContext dbCommandContext)
        => DbCommandContext = dbCommandContext;
        

        public async Task BeginTransactionAsync(
           CancellationToken cancellationToken)
        => _transaction = await DbCommandContext.Database
                .BeginTransactionAsync(cancellationToken);
        


        public async Task CommitTransactionAsync(
            CancellationToken cancellationToken)
        {
            if (_transaction is null)
                return;

            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        public async Task RollbackTransactionAsync(
            CancellationToken cancellationToken)
        {
            if (_transaction is null)
                return;

            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
