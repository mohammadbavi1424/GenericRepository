using GenericRepository.Contracts.AutoMigration.PostgreSqlMigration;
using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.AutoMigration.PostgreSqlMigration
{
    public class PostgreSqlMigrationProvider : IDatabaseMigrationProvider
    {

        public IDatabaseSchemaReader SchemaReader { get; }

        public IMigrationSqlGenerator SqlGenerator { get; }

        public IMigrationExecutor Executor { get; }






    }
}
