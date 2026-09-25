using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Models.AutoMigration
{
    public sealed class TableSnapshot
    {
        public string? Name { get; set; } = string.Empty;

        public string? Schema { get; set; } = "dbo";

        public List<ColumnSnapshot>? Columns { get; set; } = new();

        public PrimaryKeySnapshot? PrimaryKey { get; set; }

        public List<IndexSnapshot>? Indexes { get; set; } = new();

        public List<ForeignKeySnapshot>? ForeignKeys { get; set; } = new();
    }

}
