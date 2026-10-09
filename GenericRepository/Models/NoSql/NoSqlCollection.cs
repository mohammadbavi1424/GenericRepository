using System;
using System.Collections.Generic;
using System.Text;

namespace GenericRepository.Models.NoSql
{
    public class NoSqlCollection
    {
        public string Name { get; set; } = default!;

        public List<NoSqlProperty> Properties { get; set; } = [];

        public List<NoSqlIndex> Indexes { get; set; } = [];

        public NoSqlPrimaryKey? PrimaryKey { get; set; }
    }
}
