using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Contracts.AutoMigration.PostgreSqlMigration
{
    public interface IMigrationExecutor
    {
        Task ExecuteAsync(
            IReadOnlyList<string> commands,
            CancellationToken cancellationToken = default);
    }
}
