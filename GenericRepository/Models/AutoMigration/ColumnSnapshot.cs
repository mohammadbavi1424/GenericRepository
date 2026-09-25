using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Models.AutoMigration
{
    public sealed class ColumnSnapshot
    {
        public bool IsIdentity { get; set; } = false;

        public string Name { get; set; } = string.Empty;

        public string ClrType { get; set; } = string.Empty;

        public string? ColumnType { get; set; }

        public bool IsNullable { get; set; }

        public int? MaxLength { get; set; }

        public bool? IsUnicode { get; set; }

        public bool? IsFixedLength { get; set; }

        public int? Precision { get; set; }

        public int? Scale { get; set; }

        public object? DefaultValue { get; set; }

        public string? DefaultValueSql { get; set; }

        public string? ComputedColumnSql { get; set; }

        public bool? IsStored { get; set; }

        public bool IsRowVersion { get; set; }

        public Dictionary<string, object?> Annotations { get; set; }
            = new();
    }

}
