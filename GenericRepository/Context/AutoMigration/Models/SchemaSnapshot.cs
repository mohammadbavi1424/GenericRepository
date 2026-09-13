using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Context.AutoMigration.Models
{
    public class SchemaSnapshot
    {
        public List<TableSnapshot> Tables { get; set; } = [];
    }

    public class TableSnapshot
    {
        public string Schema { get; set; }
        public string Name { get; set; }

        public List<ColumnSnapshot> Columns { get; set; } = [];
    }

    public class ColumnSnapshot
    {
        public string Name { get; set; }
        public string StoreType { get; set; }
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
    }
}
