using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Contracts.AutoMigration.PostgreSqlMigration
{
    public interface IMigrationSqlGenerator
    {
        IReadOnlyList<string> Generate(
            IReadOnlyList<MigrationOperation> operations);
    }
}
