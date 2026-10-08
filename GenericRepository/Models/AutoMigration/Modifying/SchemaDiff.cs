using GenericRepository.Models.AutoMigration.Creation;
using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Models.AutoMigration.Modifying
{
    public sealed class SchemaDiff
    {
        public List<TableSnapshot> AddedTables { get; } = new();

        public List<TableSnapshotSerialized> DeletedTables { get; } = new();

        public List<TableDiff> ModifiedTables { get; } = new();
    }
}
