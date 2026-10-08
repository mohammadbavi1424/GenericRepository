using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Models.NoSql
{
    public class NoSqlProperty
    {
        public string Name { get; set; } = default!;

        public string DataType { get; set; } = default!;

        public bool IsRequired { get; set; }

        public bool IsNullable { get; set; }

        public bool IsArray { get; set; }

        public bool IsObject { get; set; }

        public string? DefaultValue { get; set; }
    }


}
