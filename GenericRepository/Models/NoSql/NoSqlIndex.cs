using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Models.NoSql
{
    public class NoSqlIndex
    {
        public string Name { get; set; } = default!;

        public List<NoSqlIndexField> Fields { get; set; } = [];

        public bool IsUnique { get; set; }

        public bool IsSparse { get; set; }
    }
}
