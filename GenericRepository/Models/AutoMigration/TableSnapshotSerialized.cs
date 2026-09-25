using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Models.AutoMigration
{
    public sealed class TableSnapshotSerialized
    {
        public int Id { get; set; }
        public string? TableName { get; set; } = string.Empty;

        public string? SchemaName { get; set; } = "dbo";

        public string? JsonColumns { get; set; }

        public string? JsonPrimaryKey { get; set; }

        public string? JsonIndexes { get; set; }

        public string? JsonForeignKeys { get; set; }
        public DateTime? UpdatedAt { get; set; }

    }


}
