using GenericRepository.Models.AutoMigration.Creation;
using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Contracts.AutoMigration.PostgreSqlMigration
{
    public interface IDatabaseSchemaReader
    {
        Task<TableSnapshot> ReadSchemaAsync(
            CancellationToken cancellationToken = default);
    }
}
