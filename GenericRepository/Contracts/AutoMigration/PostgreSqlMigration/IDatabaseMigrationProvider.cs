using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Contracts.AutoMigration.PostgreSqlMigration
{
    public interface IDatabaseMigrationProvider
    {
        IDatabaseSchemaReader SchemaReader { get; }

        IMigrationSqlGenerator SqlGenerator { get; }

        IMigrationExecutor Executor { get; }
    }
}
