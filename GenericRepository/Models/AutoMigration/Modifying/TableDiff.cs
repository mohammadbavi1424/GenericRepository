using GenericRepository.Models.AutoMigration.Creation;
using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Models.AutoMigration.Modifying
{
    public sealed class TableDiff
    {
        public TableSnapshot CurrentTable { get; set; } = null!;

        public TableSnapshotSerialized PreviousTable { get; set; } = null!;

        public List<ColumnSnapshot> AddedColumns { get; } = new();

        public List<string> DeletedColumns { get; } = new();

        public List<string> ModifiedColumns { get; } = new();

        public List<IndexSnapshot> AddedIndexes { get; } = new();

        public List<string> DeletedIndexes { get; } = new();

        public List<ForeignKeySnapshot> AddedForeignKeys { get; } = new();

        public List<string> DeletedForeignKeys { get; } = new();

    }
}
